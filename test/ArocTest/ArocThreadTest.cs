using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Aroc;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ArocTest
{
    [TestClass]
    public class ArocThreadTest
    {
        [TestMethod]
        public void GetCurrentThreadIdTest()
        {
            int threadId = ArocThread.GetCurrentThreadId();
            Assert.AreNotEqual(0, threadId);
            Assert.AreNotEqual(threadId, Thread.CurrentThread.ManagedThreadId);
        }

        [TestMethod]
        public void CurrentProcessThreadTest()
        {
            Assert.AreEqual(ArocThread.GetCurrentThreadId(), ArocThread.CurrentProcessThread.Id);
        }

        [TestMethod]
        public void PInvokeTest()
        {
            Thread thread = ArocThread.PInvoke(() =>
            {

            });

            thread.Start();
            Thread.Sleep(100);
            thread.Abort();

            int processAffinity = 2;
            int processAffinity2 = -1;
            thread = ArocThread.PInvoke(() =>
            {
                processAffinity2 = Process.GetCurrentProcess().ProcessorAffinity.ToInt32();
            }, processAffinity);

            thread.Start();
            Thread.Sleep(100);
            //Assert.AreEqual(processAffinity, processAffinity2);
            thread.Abort();
        }

        [TestMethod]
        public void CreateThreadTest()
        {
            Thread thread = ArocThread.CreateThread(() =>
            {
                for (int i = 0; i < 100; i++)
                { }
            });
            Assert.IsNotNull(thread);

            thread = ArocThread.CreateThread(obj =>
            {
                for (int i = 0; i < 100; i++)
                { }
            });

            Assert.IsNotNull(thread);
        }

        [TestMethod]
        public void ExecuteAsyncTest()
        {
            bool result = ArocThread.ExecuteAsync(obj =>
            {
                for (int i = 0; i < 100; i++)
                {
                }
            });

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CallContextTest()
        {
            string tagData = "hello world!";
            string actualyData = "";

            ArocThread.SetCallContext("test", tagData);

            // 阻止执行上下文流动
            ExecutionContext.SuppressFlow();
            ArocThread.ExecuteAsync(obj =>
            {
                try
                {
                    actualyData = Convert.ToString(ArocThread.GetCallContext("test"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
            Thread.Sleep(1000);
            ExecutionContext.RestoreFlow();

            Assert.AreNotEqual(actualyData, tagData);

            ArocThread.ExecuteAsync(obj =>
            {
                actualyData = Convert.ToString(ArocThread.GetCallContext("test"));
            });

            Thread.Sleep(1000);

            Assert.AreEqual(actualyData, tagData);
        }

        [TestMethod]
        public void CreateThreadsTest()
        {
            int t1 = 0;
            int t2 = 0;
            CancellationTokenSource cts = ArocThread.CreateThreads(
                token =>
                {
                    t1 = 100;
                    while (!token.IsCancellationRequested)
                    {
                        Thread.Sleep(500);
                    }
                    t1 = 1000;
                },
                token =>
                {
                    t2 = 100;
                    while (!token.IsCancellationRequested)
                    {
                        Thread.Sleep(500);
                    }
                    t2 = 1000;
                });

            Thread.Sleep(2000);

            Assert.AreEqual(t1, 100);
            Assert.AreEqual(t2, 100);

            cts.Cancel();

            Thread.Sleep(2000);

            Assert.AreEqual(t1, 1000);
            Assert.AreEqual(t2, 1000);
        }

        [TestMethod]
        public void ReigsterCancelCallbackTest()
        {
            CancellationTokenSource cts = ArocThread.CreateThreads(
                token =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        Thread.Sleep(1000);
                    }
                });
            int cancelTask1 = 0;
            int cancelTask2 = 0;
            ArocThread.ReigsterCancelCallback(cts.Token, false, (obj) => { cancelTask1 = 1; }, (obj) => { cancelTask2 = 1; });

            Thread.Sleep(1000);
            cts.Cancel();

            Assert.AreEqual(cancelTask1, 1);
            Assert.AreEqual(cancelTask2, 1);
        }

        [TestMethod]
        public void StartThreadsTest()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            TaskFactory tf = ArocThread.StartThreads(cts.Token, TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default,
                token => { },
                token => { }
                );

            TaskFactory<object> tfobj = ArocThread.StartThreads(cts.Token, TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default,
                token => { return new object(); },
                token => { return new object(); }
                );
        }

        [TestMethod]
        public void ExecuteTimeTest()
        {
            int times = 0;
            ArocThread.ExecuteTime(obj => { Interlocked.Add(ref times, 1); }, 1000, 1000);

            Thread.Sleep(2000);

            Assert.AreNotEqual(times, 0);

            Thread.Sleep(2000);

            Assert.IsTrue(times > 1);
        }

        [TestMethod]
        public void ExecuteOnceTimeTest()
        {
            int times = 0;
            ArocThread.ExecuteOnceTime(obj => { Interlocked.Add(ref times, 1); }, 1000, 1000);

            Thread.Sleep(2000);

            Assert.AreEqual(times, 1);

            Thread.Sleep(2000);

            Assert.AreEqual(times, 1);
        }

        public void CatchException()
        {
            try
            {
                ThreadPool.QueueUserWorkItem((obj) =>
                {
                    throw new Exception("error");
                });
            }
            catch (AggregateException aex)
            {
                // AggregateException重写了exception的GetBaseException方法，实现会返回作为问题根源的最内层的异常
                aex.GetBaseException();

                // 创建一个新的AggregateException其InnerExceptions属性包含一个异常列表，其中的异常是通过遍历原始AggregateException的内层异常层次结构生成的。
                aex.Flatten();

                // 为AggregateException包含的每个异常都调用该回调方法 该回调方法返回true来表示已经处理 返回false表示未处理
                // 如果有一个异常没有处理就创建一个新的AggregateException对象，其中只包含未处理的异常，并抛出该异常。
                aex.Handle((ex) => { Console.WriteLine("OK"); return true; });
            }
        }

        public void CancelTask()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            Task t = new Task(() =>
            {
                while (true)
                {
                    // 如果调用了Cancel函数则 会抛出OperationCanceledException异常
                    cts.Token.ThrowIfCancellationRequested();
                }
            });

            t.Start();

            try
            {
                Thread.Sleep(100);

                t.Wait();

                cts.Cancel();
            }
            catch (AggregateException aex)
            {
                // 忽略取消操作造成的异常
                aex.Handle(ex => ex is OperationCanceledException);
            }
        }

        public void ContinueTask()
        {
            Task t = new Task(() =>
            {
                new Task(() => { Console.WriteLine("first"); }).Start();
                new Task(() => { Console.WriteLine("second"); }).Start();
            });

            // t.ContinueWith(task =>Array.ForEach(),TaskContinuationOptions.AttachedToParent);
        }
    }
}
