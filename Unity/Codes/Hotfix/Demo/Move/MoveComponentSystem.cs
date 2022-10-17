using System;
using System.Collections.Generic;
using UnityEngine;

namespace ET
{
    [Timer(TimerType.MoveTimer)]
    public class MoveTimer: ATimer<MoveComponent>
    {
        public override void Run(MoveComponent self)
        {
            try
            {
                self.MoveForward(false);
            }
            catch (Exception e)
            {
                Log.Error($"move timer error: {self.Id}\n{e}");
            }
        }
    }
    
    [ObjectSystem]
    public class MoveComponentDestroySystem: DestroySystem<MoveComponent>
    {
        public override void Destroy(MoveComponent self)
        {
            self.Clear();
        }
    }

    [ObjectSystem]
    public class MoveComponentAwakeSystem: AwakeSystem<MoveComponent>
    {
        public override void Awake(MoveComponent self)
        {
            self.StartTime = 0;
            self.StartPos = Vector3.zero;
            self.NeedTime = 0;
            self.MoveTimer = 0;
            self.Callback = null;
            self.Targets.Clear();
            self.Speed = 0;
            self.N = 0;
            self.TurnTime = 0;
        }
    }

    [FriendClass(typeof(MoveComponent))]
    public static class MoveComponentSystem
    {
        public static bool IsArrived(this MoveComponent self)
        {
            return self.Targets.Count == 0;
        }

        public static bool ChangeSpeed(this MoveComponent self, float speed)
        {
            if (self.IsArrived())
            {
                return false;
            }

            if (speed < 0.0001)
            {
                return false;
            }
            
            Unit unit = self.GetParent<Unit>();

            using (ListComponent<Vector3> path = ListComponent<Vector3>.Create())
            {
                self.MoveForward(true);
                
                path.Add(unit.Position); // 第一个是Unit的pos
                for (int i = self.N; i < self.Targets.Count; ++i)
                {
                    path.Add(self.Targets[i]);
                }
                self.MoveToAsync(path, speed).Coroutine();
            }
            return true;
        }

        public static async ETTask<bool> MoveToAsync(this MoveComponent self, List<Vector3> path, float speed, int turnTime = 100, ETCancellationToken cancellationToken = null)
        {
            self.Stop();

            foreach (Vector3 v in path)
            {
                self.Targets.Add(v);
            }

            self.IsTurnHorizontal = true;
            self.TurnTime = turnTime;
            self.Speed = speed;
            ETTask<bool> tcs = ETTask<bool>.Create(true);
            self.Callback = (ret) => { tcs.SetResult(ret); };

            Game.EventSystem.Publish(new EventType.MoveStart(){Unit = self.GetParent<Unit>()});
            
            self.StartMove();
            
            void CancelAction()
            {
                self.Stop();
            }
            
            bool moveRet;
            try
            {
                cancellationToken?.Add(CancelAction);
                moveRet = await tcs;
            }
            finally
            {
                cancellationToken?.Remove(CancelAction);
            }

            if (moveRet)
            {
                Game.EventSystem.Publish(new EventType.MoveStop(){Unit = self.GetParent<Unit>()});
            }
            return moveRet;
        }

        public static void MoveForward(this MoveComponent self, bool needCancel)
        {
            ////<=========== B6699817 我改后的代码.  逻辑上清晰一些
            //Unit unit = self.GetParent<Unit>();

            ////

            //// 目标是: 给定客户端时间 t, 问在这个 t 时, 单位的 transform 信息。 
            //// 注意, 我们计算 时间预算 时, 不是  = 此次 MoveForward 时的客户端时间 - 上次 MoveForward 的客户端时间
            //// 而是每次都退回到上次确定当前 目标 pos 时的时间的。 即: = 此次 MoveForward 时的客户端时间 - 上次 确定 self.NextTarget 时的客户端时间; 

            //// 当前客户端时间 t
            //long timeNow = TimeHelper.ClientNow();
            //// self.NextTarget 为单位此时欲达到的位置 pos B (为Path中的一个节点);
            //// self.StartTime 为走到上一个 pos A,并开始往 pos B 走时纪录的客户端时间. 
            //// moveTime = timeNow - self.StartTime; 即此次 MoveForward 的时间预算。 
            //long moveTime = timeNow - self.StartTime;       


            ////在时间预算 moveTime 耗光前, 走到哪个位置. 
            //while (moveTime > 0)
            //{
            //    // 时间预算足够走到当前的目标位置 pos B
            //    if (moveTime >= self.NeedTime)
            //    {
            //        //走到当前的目标位置 pos B
            //        unit.Position = self.NextTarget;
            //        if (self.TurnTime > 0)
            //        {
            //            unit.Rotation = self.To;
            //        }

            //        //我们已走到 pos B
            //        //如果 pos B 是 Path 上最后一个节点, do callback 以告知整个MoveAsync结束, MoveForward自身也返回;
            //        //否则 找到 Path 中 pos B 的下一个节点 pos C; 
            //        if (self.N >= self.Targets.Count - 1)
            //        {
            //            unit.Position = self.NextTarget;
            //            unit.Rotation = self.To;

            //            Action<bool> callback = self.Callback;
            //            self.Callback = null;

            //            self.Clear();
            //            callback?.Invoke(!needCancel);
            //            return;
            //        }
            //        else
            //        {
            //            self.SetNextTarget();
            //        }

            //    }
            //    // 时间预算不足够走到当前的目标位置 pos B, 则计算 transform 的插值
            //    else
            //    {
            //        // 计算位置插值
            //        float amount = moveTime * 1f / self.NeedTime;
            //        if (amount > 0)
            //        {
            //            Vector3 newPos = Vector3.Lerp(self.StartPos, self.NextTarget, amount);
            //            unit.Position = newPos;
            //        }

            //        // 计算方向插值
            //        if (self.TurnTime > 0)
            //        {
            //            amount = moveTime * 1f / self.TurnTime;
            //            Quaternion q = Quaternion.Slerp(self.From, self.To, amount);
            //            unit.Rotation = q;
            //        }
            //    }

            //    // 时间预算 -= 走到 pos B 所需时间, 
            //    moveTime -= self.NeedTime;

            //}

            //===========> B6699817 我改后的代码.  逻辑上清晰一些



            #region B6699817 原作者的代码.
            //=========== B6699817 原作者的代码.

            Unit unit = self.GetParent<Unit>();

            long timeNow = TimeHelper.ClientNow();
            long moveTime = timeNow - self.StartTime;

            while (true)
            {
                if (moveTime <= 0)
                {
                    return;
                }

                // 计算位置插值
                if (moveTime >= self.NeedTime)
                {
                    unit.Position = self.NextTarget;
                    if (self.TurnTime > 0)
                    {
                        unit.Rotation = self.To;
                    }
                }
                else
                {
                    // 计算位置插值
                    float amount = moveTime * 1f / self.NeedTime;
                    if (amount > 0)
                    {
                        Vector3 newPos = Vector3.Lerp(self.StartPos, self.NextTarget, amount);
                        unit.Position = newPos;
                    }

                    // 计算方向插值
                    if (self.TurnTime > 0)
                    {
                        amount = moveTime * 1f / self.TurnTime;
                        Quaternion q = Quaternion.Slerp(self.From, self.To, amount);
                        unit.Rotation = q;
                    }
                }

                moveTime -= self.NeedTime;

                // 表示这个点还没走完，等下一帧再来
                if (moveTime < 0)
                {
                    return;
                }

                // 到这里说明这个点已经走完

                // 如果是最后一个点
                if (self.N >= self.Targets.Count - 1)
                {
                    unit.Position = self.NextTarget;
                    unit.Rotation = self.To;

                    Action<bool> callback = self.Callback;
                    self.Callback = null;

                    self.Clear();
                    callback?.Invoke(!needCancel);
                    return;
                }

                self.SetNextTarget();
            }
            #endregion
        }

        private static void StartMove(this MoveComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            
            self.BeginTime = TimeHelper.ClientNow();
            self.StartTime = self.BeginTime;
            self.SetNextTarget();

            self.MoveTimer = TimerComponent.Instance.NewFrameTimer(TimerType.MoveTimer, self);
        }

        private static void SetNextTarget(this MoveComponent self)
        {

            Unit unit = self.GetParent<Unit>();

            ++self.N;

            // 时间计算用服务端的位置, 但是移动要用客户端的位置来插值
            Vector3 v = self.GetFaceV();
            float distance = v.magnitude;
            
            // 插值的起始点要以unit的真实位置来算
            self.StartPos = unit.Position;

            self.StartTime += self.NeedTime;
            
            self.NeedTime = (long) (distance / self.Speed * 1000);

            
            if (self.TurnTime > 0)
            {
                // 要用unit的位置
                Vector3 faceV = self.GetFaceV();
                if (faceV.sqrMagnitude < 0.0001f)
                {
                    return;
                }
                self.From = unit.Rotation;
                
                if (self.IsTurnHorizontal)
                {
                    faceV.y = 0;
                }

                if (Math.Abs(faceV.x) > 0.01 || Math.Abs(faceV.z) > 0.01)
                {
                    self.To = Quaternion.LookRotation(faceV, Vector3.up);
                }

                return;
            }
            
            if (self.TurnTime == 0) // turn time == 0 立即转向
            {
                Vector3 faceV = self.GetFaceV();
                if (self.IsTurnHorizontal)
                {
                    faceV.y = 0;
                }

                if (Math.Abs(faceV.x) > 0.01 || Math.Abs(faceV.z) > 0.01)
                {
                    self.To = Quaternion.LookRotation(faceV, Vector3.up);
                    unit.Rotation = self.To;
                }
            }
        }

        private static Vector3 GetFaceV(this MoveComponent self)
        {
            return self.NextTarget - self.PreTarget;
        }

        public static bool FlashTo(this MoveComponent self, Vector3 target)
        {
            Unit unit = self.GetParent<Unit>();
            unit.Position = target;
            return true;
        }

        public static void Stop(this MoveComponent self)
        {
            if (self.Targets.Count > 0)
            {
                self.MoveForward(true);
            }

            self.Clear();
        }

        public static void Clear(this MoveComponent self)
        {
            self.StartTime = 0;
            self.StartPos = Vector3.zero;
            self.BeginTime = 0;
            self.NeedTime = 0;
            TimerComponent.Instance?.Remove(ref self.MoveTimer);
            self.Targets.Clear();
            self.Speed = 0;
            self.N = 0;
            self.TurnTime = 0;
            self.IsTurnHorizontal = false;

            if (self.Callback != null)
            {
                Action<bool> callback = self.Callback;
                self.Callback = null;
                callback.Invoke(false);
            }
        }
    }
}