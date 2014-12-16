using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aroc
{
    /// <summary>
    /// About thread
    /// </summary>
    public class ArocThread
    {
        /// <summary>
        /// 获取当前线程的物理线程ID
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        public static extern int GetCurrentThreadId();

        /// <summary>
        /// 返回物理线程的线程
        /// </summary>
        public static ProcessThread CurrentProcessThread
        {
            get
            {
                int threadId = GetCurrentThreadId();
                return (from ProcessThread pt in Process.GetCurrentProcess().Threads
                        where pt.Id == threadId
                        select pt).Single();
            }
        }

        /// <summary>
        /// 创建一个新线程
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static Thread CreateThread(Action action)
        {
            return new Thread(new ThreadStart(action));
        }

        /// <summary>
        /// 创建一个新线程
        /// </summary>
        /// <param name="action"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Thread CreateThread(Action<object> action)
        {
            return new Thread(new ParameterizedThreadStart(action));
        }

        /// <summary>
        /// P/Invoke 使用OS的物理线程来执行
        /// <param name="action">函数</param>
        /// </summary>
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlThread)]
        public static Thread PInvoke(Action action)
        {
            return new Thread(() =>
            {
                Thread.BeginThreadAffinity();
                action();
                Thread.EndThreadAffinity();
            });
        }

        /// <summary>
        ///  P/Invoke 使用OS的物理线程来执行
        /// </summary>
        /// <param name="action">函数</param>
        /// <param name="processAffinity">指定线程运行在哪个CPU核心上(4核为1,2,4,8)</param>
        /// <returns></returns>
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlThread)]
        public static Thread PInvoke(Action action, int processAffinity)
        {
            return new Thread(() =>
            {
                Thread.BeginThreadAffinity();
                CurrentProcessThread.ProcessorAffinity = new IntPtr(processAffinity);
                action();
                Thread.EndThreadAffinity();
            });
        }

        /// <summary>
        /// 在线程池执行线程
        /// </summary>
        /// <param name="action"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public static bool ExecuteAsync(Action<object> action, object state = null)
        {
            return ThreadPool.QueueUserWorkItem(new WaitCallback(action), state);
        }

        /// <summary>
        /// 设置逻辑调用上下文数据
        /// </summary>
        public static void SetCallContext(string name, object data)
        {
            CallContext.LogicalSetData(name, data);
        }

        /// <summary>
        /// 获取逻辑调用上下文数据
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static object GetCallContext(string name)
        {
            return CallContext.LogicalGetData(name);
        }

        /// <summary>
        /// 在线程池创建可取消的线程
        /// </summary>
        /// <param name="actions"></param>
        /// <returns>线程协作取消对象</returns>
        public static CancellationTokenSource CreateThreads(params Action<CancellationToken>[] actions)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            foreach (var action in actions)
            {
                ThreadPool.QueueUserWorkItem((obj) => { action(cts.Token); });
            }
            return cts;
        }

        /// <summary>
        /// 注册在取消CancellationTokenSource时调用的方法
        /// </summary>
        /// <param name="ct"></param>
        /// <param name="isSync">顺序执行/回调会被send(执行send方法，要阻塞调用线程直到到目标线程处理后才会返回，)给已捕捉的SynchronizationContext对象，由该对象决定调用哪个action</param>
        /// <param name="actions"></param>
        public static void ReigsterCancelCallback(CancellationToken ct, bool isSync, params Action<object>[] actions)
        {
            foreach (var action in actions)
            {
                ct.Register(action, isSync);
            }
        }

        /// <summary>
        /// 启动一系列Task
        /// </summary>
        /// <param name="createOption"></param>
        /// <param name="continuationOption"></param>
        /// <param name="taskScheduler"></param>
        /// <param name="actions"></param>
        /// <returns></returns>
        public static CancellationTokenSource StartThreads(TaskCreationOptions createOption, TaskContinuationOptions continuationOption, TaskScheduler taskScheduler, params Action<CancellationToken>[] actions)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            TaskFactory tf = new TaskFactory(cts.Token, createOption, continuationOption, taskScheduler);
            foreach (var action in actions)
            {
                tf.StartNew(() => { action(cts.Token); });
            }
            return cts;
        }

        /// <summary>
        /// 启动一系列Task
        /// </summary>
        /// <param name="createOption"></param>
        /// <param name="continuationOption"></param>
        /// <param name="taskScheduler"></param>
        /// <param name="actions"></param>
        /// <returns></returns>
        public static CancellationTokenSource StartThreads(TaskCreationOptions createOption, TaskContinuationOptions continuationOption, TaskScheduler taskScheduler, params Func<CancellationToken, object>[] actions)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            TaskFactory<object> tf = new TaskFactory<object>(cts.Token, createOption, continuationOption, taskScheduler);
            foreach (var action in actions)
            {
                tf.StartNew(() => action(cts.Token));
            }
            return cts;
        }

        public static Timer ExecuteTime(Action<object> action, object state = null)
        {
            return null;
        }

        public static Timer ExecuteOnceTime(Action<object> action)
        {
            return null;
        }
    }
}

