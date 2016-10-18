using SwarmSight.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SwarmSight.VideoPlayer.Pipeline
{
    public abstract class PipelineWorker
    {
        public enum WorkerState
        {
            Stopped,
            Working,
            Paused
        }

        public bool Work;
        public bool Pause;

        public WorkerState State;
        public FrameBuffer Queue;
        public int QueuePosition = 0;
        

        public abstract void BeginWork();
        public abstract bool IsCurrentItemReadyForWork();
        public abstract void WorkOnCurrentItem();
        public abstract void FinishWork();

        private Thread workerThread;
        public bool DoOneItem = false;

        public Frame CurrentItem
        {
            get
            {
                if (Queue == null)
                    return null;

                lock(Queue)
                {
                    if (Queue.Count == 0)
                        return null;

                    return Queue.Get(QueuePosition);
                }
            }
        }

        public void StartWorking(bool oneItem = false)
        {
            StopWorking();

            while (State != WorkerState.Stopped)
                Thread.Sleep(10);

            QueuePosition = 0;
            Work = true;
            Pause = false;
            DoOneItem = oneItem;
            State = WorkerState.Working;

            workerThread = new Thread(WorkLoop) { IsBackground = true };
            workerThread.Name = this.ToString() + " WorkLoop";
            workerThread.Start();
        }

        public void AdvancePosition()
        {
            lock(Queue)
            {
                QueuePosition = Queue.GetNextPosition(QueuePosition);
            }
        }

        public void StopWorking()
        {
            Work = false;
            Pause = false;
        }

        public void PauseWork()
        {
            Work = true;
            Pause = true;
        }

        public void ResumeWork(bool oneItem = false)
        {
            DoOneItem = oneItem;
            Pause = false;
            Work = true;
        }

        public void WorkLoop()
        {
            State = WorkerState.Working;

            BeginWork();

            while (Work)
            {
                if (CurrentItem != null && IsCurrentItemReadyForWork())
                {
                    WorkOnCurrentItem();

                    AdvancePosition();

                    if (DoOneItem)
                    {
                        PauseWork();
                        DoOneItem = false;
                    }
                }
                else
                    Thread.Sleep(5);
                    
                if(Pause)
                {
                    State = WorkerState.Paused;

                    while (Pause)
                    {
                        Thread.Sleep(30);
                    }
                }
            }

            FinishWork();

            QueuePosition = 0;
            State = WorkerState.Stopped;
        }

        
    }
}
