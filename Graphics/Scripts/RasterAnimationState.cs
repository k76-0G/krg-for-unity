﻿using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace KRG {

    public class RasterAnimationState {

#region delegates

        public delegate void Handler(RasterAnimationState ras);

#endregion

#region fields

        /// <summary>
        /// Occurs when a frame sequence starts.
        /// </summary>
        event Handler _frameSequenceStartHandlers;

        /// <summary>
        /// Occurs when a frame sequence stops.
        /// </summary>
        event Handler _frameSequenceStopHandlers;

        /// <summary>
        /// Occurs when a frame sequence starts or after the play index is incremented.
        /// </summary>
        event Handler _frameLoopStartHandlers;

        /// <summary>
        /// Occurs when a frame sequence stops or before the play index is incremented.
        /// </summary>
        event Handler _frameLoopStopHandlers;

        /// <summary>
        /// The raster animation scriptable object.
        /// </summary>
        RasterAnimation _rasterAnimation;

        /// <summary>
        /// The raster animation info (optional debugging tool).
        /// </summary>
        RasterAnimationInfo _rasterAnimationInfo;

        /// <summary>
        /// The current loop index. If the raster animation's "Loop" setting is unchecked, this will always be 0.
        /// </summary>
        int _loopIndex;

        /// <summary>
        /// The current frame sequence index (zero-based).
        /// </summary>
        int _frameSequenceIndex;

        /// <summary>
        /// The name of the current frame sequence.
        /// </summary>
        protected string _frameSequenceName;

        /// <summary>
        /// The ordered list of frames to play in the current frame sequence.
        /// </summary>
        ReadOnlyCollection<int> _frameSequenceFrameList;

        /// <summary>
        /// The current frame sequence's From Frame (one-based; may be a cached random value).
        /// </summary>
        int _frameSequenceFromFrame;

        /// <summary>
        /// The current frame sequence's To Frame (one-based; may be a cached random value).
        /// </summary>
        int _frameSequenceToFrame;

        /// <summary>
        /// The current frame sequence's play count (may be a cached random value).
        /// </summary>
        int _frameSequencePlayCount;

        /// <summary>
        /// The current frame sequence's play index (zero-based).
        /// </summary>
        int _frameSequencePlayIndex;

#endregion

#region properties

        public virtual int frameSequenceIndex { get { return _frameSequenceIndex; } }

        public virtual string frameSequenceName { get { return _frameSequenceName; } }

        public virtual int frameSequenceFromFrame {
            get { return frameSequenceHasFrameList ? _frameSequenceFrameList[0] : _frameSequenceFromFrame; }
        }

        public virtual bool frameSequenceHasFrameList {
            //this function will probably go away soon after I integrate things better, as it should always be true
            get { return _frameSequenceFrameList != null && _frameSequenceFrameList.Count > 0; }
        }

        public virtual RasterAnimation rasterAnimation { get { return _rasterAnimation; } }

        public List<int> FrameSequencePreActions { get; private set; }

        public AudioPlayStyle FrameSequenceAudioPlayStyle { get; private set; }

        public string FrameSequenceAudioEvent { get; private set; }

#endregion

#region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="KRG.RasterAnimationState"/> class.
        /// </summary>
        /// <param name="rasterAnimation">The raster animation.</param>
        /// <param name="frameSequenceStartHandler">An optional frame sequence start handler.</param>
        /// <param name="frameSequenceStopHandler">An optional frame sequence stop handler.</param>
        public RasterAnimationState(
            RasterAnimation rasterAnimation,
            Handler frameSequenceStartHandler = null,
            Handler frameSequenceStopHandler = null,
            Handler frameLoopStartHandler = null,
            Handler frameLoopStopHandler = null
        ) {
            _rasterAnimation = rasterAnimation;
            G.U.Require(_rasterAnimation, "Raster Animation", "this Raster Animation State");
            _frameSequenceStartHandlers = frameSequenceStartHandler;
            _frameSequenceStopHandlers = frameSequenceStopHandler;
            _frameLoopStartHandlers = frameLoopStartHandler;
            _frameLoopStopHandlers = frameLoopStopHandler;
            Reset();
        }

#endregion

#region public methods

        /// <summary>
        /// Advances the frame number. This uses the new frame list which allows for playing frames in any order.
        /// </summary>
        /// <returns><c>true</c>, if the animation should continue playing, <c>false</c> otherwise.</returns>
        /// <param name="frameListIndex">Frame list index.</param>
        /// <param name="frameNumber">Frame number (one-based).</param>
        public virtual bool AdvanceFrame(ref int frameListIndex, out int frameNumber) {
            frameNumber = 0;
            if (!frameSequenceHasFrameList) {
                G.U.Err("Wrong function. Use AdvanceFrame(ref int frameNumber) instead.");
                return false;
            }
            if (frameListIndex < _frameSequenceFrameList.Count - 1) {
                frameNumber = _frameSequenceFrameList[++frameListIndex];
            } else if (_frameSequencePlayIndex < _frameSequencePlayCount - 1) {
                _frameLoopStopHandlers?.Invoke(this);
                ++_frameSequencePlayIndex;
                frameListIndex = 0;
                frameNumber = _frameSequenceFrameList[0];
                _frameLoopStartHandlers?.Invoke(this);
            } else if (_frameSequenceIndex < _rasterAnimation.frameSequenceCount - 1) {
                InvokeFrameSequenceStopHandlers();
                SetFrameSequence(_frameSequenceIndex + 1);
                frameListIndex = 0;
                frameNumber = _frameSequenceFrameList[0];
            } else if (_rasterAnimation.DoesLoop(_loopIndex)) {
                InvokeFrameSequenceStopHandlers();
                ++_loopIndex;
                SetFrameSequence(_rasterAnimation.loopToSequence);
                frameListIndex = 0;
                frameNumber = _frameSequenceFrameList[0];
            } else {
                InvokeFrameSequenceStopHandlers();
                //the animation has finished playing
                return false;
            }
            RefreshRasterAnimationInfo(frameNumber); //TODO: update parameters as needed
            return true;
        }

        /// <summary>
        /// DEPRECATED - Advances the frame number. Currently this only supports moving forward by a single frame.
        /// </summary>
        /// <returns><c>true</c>, if the animation should continue playing, <c>false</c> otherwise.</returns>
        /// <param name="frameNumber">Frame number (one-based).</param>
        public virtual bool AdvanceFrame(ref int frameNumber) {
            if (frameSequenceHasFrameList) {
                G.U.Err("Wrong function. Use AdvanceFrame(ref int frameListIndex, out int frameNumber) instead.");
                return false;
            }
            if (frameNumber < _frameSequenceToFrame) {
                ++frameNumber;
            } else if (_frameSequencePlayIndex < _frameSequencePlayCount - 1) {
                _frameLoopStopHandlers?.Invoke(this);
                ++_frameSequencePlayIndex;
                frameNumber = _frameSequenceFromFrame;
                _frameLoopStartHandlers?.Invoke(this);
            } else if (_frameSequenceIndex < _rasterAnimation.frameSequenceCount - 1) {
                InvokeFrameSequenceStopHandlers();
                SetFrameSequence(_frameSequenceIndex + 1);
                frameNumber = _frameSequenceFromFrame;
            } else if (_rasterAnimation.DoesLoop(_loopIndex)) {
                InvokeFrameSequenceStopHandlers();
                ++_loopIndex;
                SetFrameSequence(_rasterAnimation.loopToSequence);
                frameNumber = _frameSequenceFromFrame;
            } else {
                InvokeFrameSequenceStopHandlers();
                //the animation has finished playing
                return false;
            }
            RefreshRasterAnimationInfo(frameNumber);
            return true;
        }

        /// <summary>
        /// Advances the frame sequence.
        /// NOTE: This is all copied from AdvanceFrame(ref int frameListIndex, out int frameNumber)
        /// </summary>
        /// <returns><c>true</c>, if the animation should continue playing, <c>false</c> otherwise.</returns>
        /// <param name="frameListIndex">Frame list index.</param>
        /// <param name="frameNumber">Frame number (one-based).</param>
        public virtual bool AdvanceFrameSequence(ref int frameListIndex, out int frameNumber)
        {
            frameNumber = 0;
            if (!frameSequenceHasFrameList)
            {
                G.U.Err("AdvanceFrameSequence can only be used if the frame sequence has a frame list.");
                return false;
            }
            if (_frameSequenceIndex < _rasterAnimation.frameSequenceCount - 1)
            {
                InvokeFrameSequenceStopHandlers();
                SetFrameSequence(_frameSequenceIndex + 1);
                frameListIndex = 0;
                frameNumber = _frameSequenceFrameList[0];
            }
            else if (_rasterAnimation.DoesLoop(_loopIndex))
            {
                InvokeFrameSequenceStopHandlers();
                ++_loopIndex;
                SetFrameSequence(_rasterAnimation.loopToSequence);
                frameListIndex = 0;
                frameNumber = _frameSequenceFrameList[0];
            }
            else
            {
                InvokeFrameSequenceStopHandlers();
                //the animation has finished playing
                return false;
            }
            RefreshRasterAnimationInfo(frameNumber); //TODO: update parameters as needed
            return true;
        }

        public void Reset() {
            SetFrameSequence(0);
        }

#endregion

#region protected methods

        protected virtual void RefreshRasterAnimationInfo(int frameNumber) {
            RasterAnimationInfo i = _rasterAnimationInfo;
            if (i == null) return;
            i.frameSequenceName = _frameSequenceName;
            //TODO: add frame list (_frameSequenceFrameList) to raster animation info
            //TODO: add frame list index (_frameSequenceFrameListIndex) to raster animation info
            i.frameSequenceFromFrameMin = _rasterAnimation.GetFrameSequenceFromFrameMin(_frameSequenceIndex);
            i.frameSequenceFromFrame = _frameSequenceFromFrame;
            i.frameSequenceFromFrameMax = _rasterAnimation.GetFrameSequenceFromFrameMax(_frameSequenceIndex);
            i.frameSequenceToFrameMin = _rasterAnimation.GetFrameSequenceToFrameMin(_frameSequenceIndex);
            i.frameSequenceToFrame = _frameSequenceToFrame;
            i.frameSequenceToFrameMax = _rasterAnimation.GetFrameSequenceToFrameMax(_frameSequenceIndex);
            i.frameSequencePlayCountMin = _rasterAnimation.GetFrameSequencePlayCountMin(_frameSequenceIndex);
            i.frameSequencePlayCount = _frameSequencePlayCount;
            i.frameSequencePlayCountMax = _rasterAnimation.GetFrameSequencePlayCountMax(_frameSequenceIndex);
            i.frameSequenceCount = _rasterAnimation.frameSequenceCount;
            i.frameSequenceIndex = _frameSequenceIndex;
            i.frameSequencePlayIndex = _frameSequencePlayIndex;
            i.frameNumber = frameNumber;
            i.Refresh();
        }

        /// <summary>
        /// Sets the frame sequence, or if not playable, advances to the next playable frame sequence.
        /// </summary>
        /// <param name="frameSequenceIndex">Frame sequence index.</param>
        protected virtual void SetFrameSequence(int frameSequenceIndex) {
            if (!_rasterAnimation.hasPlayableFrameSequences) {
                G.U.Err("This Raster Animation must have playable Frame Sequences.", this, _rasterAnimation);
                return;
            }
            int playCount, fsLoopCount = 0;
            while (true) {
                if (frameSequenceIndex >= _rasterAnimation.frameSequenceCount) {
                    frameSequenceIndex = 0;
                }
                playCount = _rasterAnimation.GetFrameSequencePlayCount(frameSequenceIndex);
                if (playCount > 0) {
                    break;
                }
                frameSequenceIndex++;
                fsLoopCount++;
                if (fsLoopCount >= _rasterAnimation.frameSequenceCountMax) {
                    G.U.Err("Stuck in an infinite loop.", this, _rasterAnimation);
                    return;
                }
            }
            _frameSequenceIndex = frameSequenceIndex;
            _frameSequenceName = _rasterAnimation.GetFrameSequenceName(frameSequenceIndex);
            _frameSequenceFrameList = _rasterAnimation.GetFrameSequenceFrameList(frameSequenceIndex);
            _frameSequenceFromFrame = _rasterAnimation.GetFrameSequenceFromFrame(frameSequenceIndex);
            _frameSequenceToFrame = _rasterAnimation.GetFrameSequenceToFrame(frameSequenceIndex);
            _frameSequencePlayCount = playCount;
            _frameSequencePlayIndex = 0;
            FrameSequencePreActions = _rasterAnimation.GetFrameSequencePreActions(frameSequenceIndex);
            FrameSequenceAudioPlayStyle = _rasterAnimation.GetFrameSequenceAudioPlayStyle(frameSequenceIndex);
            FrameSequenceAudioEvent = _rasterAnimation.GetFrameSequenceAudioEvent(frameSequenceIndex);
            RefreshRasterAnimationInfo(_frameSequenceFromFrame);
            InvokeFrameSequenceStartHandlers();
        }

#endregion

#region handler methods

        public void AddFrameSequenceStartHandler(Handler frameSequenceStartHandler) {
            _frameSequenceStartHandlers += frameSequenceStartHandler;
        }

        public void AddFrameSequenceStopHandler(Handler frameSequenceStopHandler) {
            _frameSequenceStopHandlers += frameSequenceStopHandler;
        }

        public void RemoveFrameSequenceStartHandler(Handler frameSequenceStartHandler) {
            _frameSequenceStartHandlers -= frameSequenceStartHandler;
        }

        public void RemoveFrameSequenceStopHandler(Handler frameSequenceStopHandler) {
            _frameSequenceStopHandlers -= frameSequenceStopHandler;
        }

        private void InvokeFrameSequenceStartHandlers()
        {
            _frameSequenceStartHandlers?.Invoke(this);
            _frameLoopStartHandlers?.Invoke(this);
        }

        private void InvokeFrameSequenceStopHandlers()
        {
            _frameLoopStopHandlers?.Invoke(this);
            _frameSequenceStopHandlers?.Invoke(this);
        }

#endregion

    }
}
