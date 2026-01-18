using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using JetBrains.Annotations;
using Object = UnityEngine.Object;

namespace Core.Timer 
{ 
    public class Timer
    {
        #region Public Properties/Fields

        /// <summary>
        /// 计时器从开始到结束所需的总时长（秒）。
        /// </summary>
        public float Duration { get; private set; }

        /// <summary>
        /// 计时器是否在完成后自动再次运行（循环）。
        /// </summary>
        public bool IsLooped { get; set; }

        /// <summary>
        /// 计时器是否已经正常完成。如果计时器被取消，则该值为 false。
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// 计时器是否使用真实时间（real-time）或游戏时间（game-time）。
        /// 真实时间不受 Time.timeScale 影响（暂停、慢动作等），
        /// 游戏时间会受 Time.timeScale 影响。
        /// </summary>
        public bool UsesRealTime { get; private set; }

        /// <summary>
        /// 计时器当前是否处于暂停状态。
        /// </summary>
        public bool IsPaused
        {
            get { return this._timeElapsedBeforePause.HasValue; }
        }

        /// <summary>
        /// 计时器是否已被取消。
        /// </summary>
        public bool IsCancelled
        {
            get { return this._timeElapsedBeforeCancel.HasValue; }
        }

        /// <summary>
        /// 计时器是否因任意原因结束（完成、取消、或绑定对象被销毁）。
        /// </summary>
        public bool IsDone
        {
            get { return this.IsCompleted || this.IsCancelled || this.IsOwnerDestroyed; }
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// 注册一个新的计时器，在指定时间（秒）后触发回调。
        ///
        /// 注册的计时器会在场景切换时被销毁。
        /// </summary>
        /// <param name="duration">计时器等待的时长（秒）。</param>
        /// <param name="onComplete">计时器完成时执行的回调。</param>
        /// <param name="onUpdate">计时器每帧更新时执行的回调，参数为当前循环已过去的时间（秒）。</param>
        /// <param name="isLooped">计时器是否循环执行。</param>
        /// <param name="useRealTime">是否使用真实时间（不受 Time.timeScale 影响）。</param>
        /// <param name="autoDestroyOwner">
        /// 绑定的 MonoBehaviour。当该对象被销毁时，计时器会自动失效，
        /// 避免在对象销毁后访问其组件导致 NullReferenceException。
        /// </param>
        /// <returns>返回 Timer 对象，可用于暂停、恢复、取消等操作。</returns>
        public static Timer Register(float duration, Action onComplete, Action<float> onUpdate = null,
            bool isLooped = false, bool useRealTime = false, MonoBehaviour autoDestroyOwner = null)
        {
            // 如果 TimerManager 不存在，则创建一个用于更新所有计时器的管理器对象。
            if (Timer._manager == null)
            {
                TimerManager managerInScene = Object.FindObjectOfType<TimerManager>();
                if (managerInScene != null)
                {
                    Timer._manager = managerInScene;
                }
                else
                {
                    GameObject managerObject = new GameObject { name = "TimerManager" };
                    Timer._manager = managerObject.AddComponent<TimerManager>();
                }
            }

            Timer timer = new Timer(duration, onComplete, onUpdate, isLooped, useRealTime, autoDestroyOwner);
            Timer._manager.RegisterTimer(timer);
            return timer;
        }

        /// <summary>
        /// 取消计时器。与实例方法相比，这里不会因为 timer 为 null 而抛异常。
        /// </summary>
        public static void Cancel(Timer timer)
        {
            if (timer != null)
            {
                timer.Cancel();
            }
        }

        /// <summary>
        /// 暂停计时器。不会因为 timer 为 null 而抛异常。
        /// </summary>
        public static void Pause(Timer timer)
        {
            if (timer != null)
            {
                timer.Pause();
            }
        }

        /// <summary>
        /// 恢复计时器。不会因为 timer 为 null 而抛异常。
        /// </summary>
        public static void Resume(Timer timer)
        {
            if (timer != null)
            {
                timer.Resume();
            }
        }

        public static void CancelAllRegisteredTimers()
        {
            if (Timer._manager != null)
            {
                Timer._manager.CancelAllTimers();
            }
        }

        public static void PauseAllRegisteredTimers()
        {
            if (Timer._manager != null)
            {
                Timer._manager.PauseAllTimers();
            }
        }

        public static void ResumeAllRegisteredTimers()
        {
            if (Timer._manager != null)
            {
                Timer._manager.ResumeAllTimers();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 停止正在运行或暂停的计时器。完成回调不会被调用。
        /// </summary>
        public void Cancel()
        {
            if (this.IsDone)
            {
                return;
            }

            this._timeElapsedBeforeCancel = this.GetTimeElapsed();
            this._timeElapsedBeforePause = null;
        }

        /// <summary>
        /// 暂停计时器。暂停后可从相同进度继续。
        /// </summary>
        public void Pause()
        {
            if (this.IsPaused || this.IsDone)
            {
                return;
            }

            this._timeElapsedBeforePause = this.GetTimeElapsed();
        }

        /// <summary>
        /// 恢复暂停的计时器。如果计时器未暂停则无效果。
        /// </summary>
        public void Resume()
        {
            if (!this.IsPaused || this.IsDone)
            {
                return;
            }

            this._timeElapsedBeforePause = null;
        }

        /// <summary>
        /// 获取当前循环已过去的时间（秒）。
        /// </summary>
        /// <returns>
        /// - 若计时器完成，则等于 duration  
        /// - 若取消/暂停，则等于取消/暂停时的已过时间  
        /// - 否则为当前世界时间 - 开始时间
        /// </returns>
        public float GetTimeElapsed()
        {
            if (this.IsCompleted || this.GetWorldTime() >= this.GetFireTime())
            {
                return this.Duration;
            }

            return this._timeElapsedBeforeCancel ??
                   this._timeElapsedBeforePause ??
                   this.GetWorldTime() - this._startTime;
        }

        /// <summary>
        /// 获取计时器剩余时间（秒）。
        /// </summary>
        public float GetTimeRemaining()
        {
            return this.Duration - this.GetTimeElapsed();
        }

        /// <summary>
        /// 获取计时器完成进度（0~1）。
        /// </summary>
        public float GetRatioComplete()
        {
            return this.GetTimeElapsed() / this.Duration;
        }

        /// <summary>
        /// 获取计时器剩余进度（0~1）。
        /// </summary>
        public float GetRatioRemaining()
        {
            return this.GetTimeRemaining() / this.Duration;
        }

        #endregion

        #region Private Static Properties/Fields

        // 负责更新所有注册的计时器
        private static TimerManager _manager;

        #endregion

        #region Private Properties/Fields

        private bool IsOwnerDestroyed
        {
            get { return this._hasAutoDestroyOwner && this._autoDestroyOwner == null; }
        }

        private readonly Action _onComplete;
        private readonly Action<float> _onUpdate;
        private float _startTime;
        private float _lastUpdateTime;

        // 暂停/取消时记录已过时间，避免 startTime 被修改后计算错误
        private float? _timeElapsedBeforeCancel;
        private float? _timeElapsedBeforePause;

        // 绑定的 MonoBehaviour 被销毁后，计时器自动失效
        private readonly MonoBehaviour _autoDestroyOwner;
        private readonly bool _hasAutoDestroyOwner;

        #endregion

        #region Private Constructor

        /// <summary>
        /// 私有构造函数（只能通过 Register 创建）。
        /// </summary>
        private Timer(float duration, Action onComplete, Action<float> onUpdate,
            bool isLooped, bool usesRealTime, MonoBehaviour autoDestroyOwner)
        {
            this.Duration = duration;
            this._onComplete = onComplete;
            this._onUpdate = onUpdate;

            this.IsLooped = isLooped;
            this.UsesRealTime = usesRealTime;

            this._autoDestroyOwner = autoDestroyOwner;
            this._hasAutoDestroyOwner = autoDestroyOwner != null;

            this._startTime = this.GetWorldTime();
            this._lastUpdateTime = this._startTime;
        }

        #endregion

        #region Private Methods

        private float GetWorldTime()
        {
            return this.UsesRealTime ? Time.realtimeSinceStartup : Time.time;
        }

        private float GetFireTime()
        {
            return this._startTime + this.Duration;
        }

        private float GetTimeDelta()
        {
            return this.GetWorldTime() - this._lastUpdateTime;
        }

        private void Update()
        {
            if (this.IsDone)
            {
                return;
            }

            if (this.IsPaused)
            {
                this._startTime += this.GetTimeDelta();
                this._lastUpdateTime = this.GetWorldTime();
                return;
            }

            this._lastUpdateTime = this.GetWorldTime();

            if (this._onUpdate != null)
            {
                this._onUpdate(this.GetTimeElapsed());
            }

            if (this.GetWorldTime() >= this.GetFireTime())
            {
                if (this._onComplete != null)
                {
                    this._onComplete();
                }

                if (this.IsLooped)
                {
                    this._startTime = this.GetWorldTime();
                }
                else
                {
                    this.IsCompleted = true;
                }
            }
        }

        #endregion

        #region Manager Class

        /// <summary>
        /// 管理所有正在运行的 Timer。
        /// 第一次创建 Timer 时自动生成，不需要手动放进场景。
        /// </summary>
        private class TimerManager : MonoBehaviour
        {
            private List<Timer> _timers = new List<Timer>();

            // 缓存待添加的计时器，避免在遍历时修改集合
            private List<Timer> _timersToAdd = new List<Timer>();

            public void RegisterTimer(Timer timer)
            {
                this._timersToAdd.Add(timer);
            }

            public void CancelAllTimers()
            {
                foreach (Timer timer in this._timers)
                {
                    timer.Cancel();
                }

                this._timers = new List<Timer>();
                this._timersToAdd = new List<Timer>();
            }

            public void PauseAllTimers()
            {
                foreach (Timer timer in this._timers)
                {
                    timer.Pause();
                }
            }

            public void ResumeAllTimers()
            {
                foreach (Timer timer in this._timers)
                {
                    timer.Resume();
                }
            }

            // 每帧更新所有计时器
            private void Update()
            {
                this.UpdateAllTimers();
            }

            private void UpdateAllTimers()
            {
                if (this._timersToAdd.Count > 0)
                {
                    this._timers.AddRange(this._timersToAdd);
                    this._timersToAdd.Clear();
                }

                foreach (Timer timer in this._timers)
                {
                    timer.Update();
                }

                this._timers.RemoveAll(t => t.IsDone);
            }
        }
        #endregion
    }
    
}
