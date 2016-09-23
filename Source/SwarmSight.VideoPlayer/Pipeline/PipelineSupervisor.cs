using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SwarmSight.VideoPlayer.Pipeline
{
    public abstract class PipelineSupervisor
    {
        public WorkState State;
        public event EventHandler WorkFinished;
        public Thread EndOfWorkMonitor;
        public Thread EndOfOneItemMonitor;
        public bool ContinueEndOfWorLoadMonitoring;
        public PipelineWorker[] Workers;

        public abstract bool IsAtEndOfWorkLoad();

        public void StartWorking(bool oneFrame = false)
        {
            if (State == WorkState.Working)
                return;

            if (State == WorkState.Paused)
            {
                State = WorkState.Working;

                for (int w = 0; w < Workers.Length; w++)
                {
                    Workers[w].ResumeWork(oneFrame);
                }

                return;
            }

            if (State == WorkState.Stopped)
            {
                State = WorkState.Working;

                for (int w = 0; w < Workers.Length; w++)
                {
                    Workers[w].StartWorking(oneFrame);
                }

                MonitorEndOfWorkLoad();
            }

            if (oneFrame)
                MonitorEndOfOneItem();
        }

        public void PauseWork()
        {
            State = WorkState.Paused;

            for (int w = 0; w < Workers.Length; w++)
            {
                Workers[w].PauseWork();
            }
        }

        public void StopWorking()
        {
            ContinueEndOfWorLoadMonitoring = false;

            for (int w = 0; w < Workers.Length; w++)
            {
                Workers[w].StopWorking();
            }

            State = WorkState.Stopped;
        }

        public void MonitorEndOfWorkLoad()
        {
            ContinueEndOfWorLoadMonitoring = true;

            EndOfWorkMonitor = new Thread(() =>
            {
                while (ContinueEndOfWorLoadMonitoring && !IsAtEndOfWorkLoad())
                    Thread.Sleep(10);

                if (IsAtEndOfWorkLoad())
                    StopWorking();

                while (Workers.Any(w => w.State != PipelineWorker.WorkerState.Stopped))
                    Thread.Sleep(10);

                if (WorkFinished != null)
                    WorkFinished(this, null);
            });

            EndOfWorkMonitor.IsBackground = true;
            EndOfWorkMonitor.Start();
        }

        public void MonitorEndOfOneItem()
        {
            EndOfOneItemMonitor = new Thread(() =>
            {
                while (Workers.Any(w => w.DoOneItem || w.State == PipelineWorker.WorkerState.Working))
                    Thread.Sleep(10);

                State = WorkState.Paused;
            });

            EndOfOneItemMonitor.IsBackground = true;
            EndOfOneItemMonitor.Start();
        }
        
    }
}
