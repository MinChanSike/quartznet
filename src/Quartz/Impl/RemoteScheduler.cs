#region License

/* 
 * All content copyright Terracotta, Inc., unless otherwise indicated. All rights reserved. 
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not 
 * use this file except in compliance with the License. You may obtain a copy 
 * of the License at 
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0 
 *   
 * Unless required by applicable law or agreed to in writing, software 
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations 
 * under the License.
 * 
 */

#endregion

using System;
using System.Collections.Generic;
using System.Threading;
#if REMOTING
using System.Runtime.Remoting;
#endif // REMOTING
using System.Threading.Tasks;

using Quartz.Core;
using Quartz.Impl.Matchers;
using Quartz.Simpl;
using Quartz.Spi;

namespace Quartz.Impl
{
    /// <summary>
    /// An implementation of the <see cref="IScheduler" /> interface that remotely
    /// proxies all method calls to the equivalent call on a given <see cref="QuartzScheduler" />
    /// instance, via remoting or similar technology.
    /// </summary>
    /// <seealso cref="IScheduler" />
    /// <seealso cref="QuartzScheduler" />
    /// <author>James House</author>
    /// <author>Marko Lahma (.NET)</author>
    public class RemoteScheduler : IScheduler
    {
        private IRemotableQuartzScheduler rsched;
        private readonly string schedId;
        private readonly IRemotableSchedulerProxyFactory proxyFactory;

        /// <summary>
        /// Construct a <see cref="RemoteScheduler" /> instance to proxy the given
        /// RemoteableQuartzScheduler instance.
        /// </summary>
        public RemoteScheduler(string schedId, IRemotableSchedulerProxyFactory proxyFactory)
        {
            this.schedId = schedId;
            this.proxyFactory = proxyFactory;
        }

        /// <summary>
        /// returns true if the given JobGroup
        /// is paused
        /// </summary>
        public virtual Task<bool> IsJobGroupPaused(
            string groupName, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.IsJobGroupPaused(groupName, cancellationToken));
        }

        /// <summary>
        /// returns true if the given TriggerGroup
        /// is paused
        /// </summary>
        public virtual Task<bool> IsTriggerGroupPaused(
            string groupName, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.IsTriggerGroupPaused(groupName, cancellationToken));
        }

        /// <summary>
        /// Returns the name of the <see cref="IScheduler" />.
        /// </summary>
        public virtual string SchedulerName
        {
            get { return ReadPropertyInGuard(x => x.SchedulerName); }
        }

        /// <summary>
        /// Returns the instance Id of the <see cref="IScheduler" />.
        /// </summary>
        public virtual string SchedulerInstanceId
        {
            get { return ReadPropertyInGuard(x => x.SchedulerInstanceId); }
        }

        /// <summary>
        /// Get a <see cref="SchedulerMetaData"/> object describing the settings
        /// and capabilities of the scheduler instance.
        /// <para>
        /// Note that the data returned is an 'instantaneous' snap-shot, and that as
        /// soon as it's returned, the meta data values may be different.
        /// </para>
        /// </summary>
        /// <returns></returns>
        public virtual Task<SchedulerMetaData> GetMetaData(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => Task.FromResult(new SchedulerMetaData(SchedulerName, SchedulerInstanceId, GetType(), true, IsStarted, InStandbyMode,
                                                          IsShutdown, x.RunningSince, x.NumJobsExecuted, x.JobStoreClass,
                                                          x.SupportsPersistence, x.Clustered, x.ThreadPoolClass, x.ThreadPoolSize, x.Version)));
        }

        /// <summary> 
        /// Returns the <see cref="SchedulerContext" /> of the <see cref="IScheduler" />.
        /// </summary>
        public virtual SchedulerContext Context
        {
            get { return ReadPropertyInGuard(x => x.SchedulerContext); }
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual bool InStandbyMode
        {
            get { return ReadPropertyInGuard(x => x.InStandbyMode); }
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual bool IsShutdown
        {
            get { return ReadPropertyInGuard(x => x.IsShutdown); }
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task<IReadOnlyList<IJobExecutionContext>> GetCurrentlyExecutingJobs(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(ReadPropertyInGuard(x => x.CurrentlyExecutingJobs));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task<IReadOnlyList<string>> GetJobGroupNames(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.GetJobGroupNames(cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task<IReadOnlyList<string>> GetTriggerGroupNames(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.GetTriggerGroupNames(cancellationToken));
        }

        /// <summary>
        /// Get the names of all <see cref="ITrigger" /> groups that are paused.
        /// </summary>
        /// <value></value>
        public virtual Task<ISet<string>> GetPausedTriggerGroups(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.GetPausedTriggerGroups(cancellationToken));
        }

        /// <summary>
        /// Set the <see cref="JobFactory" /> that will be responsible for producing
        /// instances of <see cref="IJob" /> classes.
        /// <para>
        /// JobFactories may be of use to those wishing to have their application
        /// produce <see cref="IJob" /> instances via some special mechanism, such as to
        /// give the opportunity for dependency injection.
        /// </para>
        /// </summary>
        /// <value></value>
        /// <seealso cref="IJobFactory"/>
        /// <throws>  SchedulerException </throws>
        public virtual IJobFactory JobFactory
        {
            set { throw new SchedulerException("Operation not supported for remote schedulers."); }
        }

        /// <summary> 
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task Start(CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.Start(cancellationToken));
        }

        /// <summary> 
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public Task StartDelayed(TimeSpan delay, CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.StartDelayed(delay, cancellationToken));
        }

        /// <summary>
        /// Whether the scheduler has been started.
        /// </summary>
        /// <value></value>
        /// <remarks>
        /// Note: This only reflects whether <see cref="Start"/> has ever
        /// been called on this Scheduler, so it will return <see langword="true" /> even
        /// if the <see cref="IScheduler" /> is currently in standby mode or has been
        /// since shutdown.
        /// </remarks>
        /// <seealso cref="Start"/>
        /// <seealso cref="IsShutdown"/>
        /// <seealso cref="InStandbyMode"/>
        public virtual bool IsStarted
        {
            get { return ReadPropertyInGuard(x => x.RunningSince.HasValue); }
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task Standby(CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.Standby(cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual async Task Shutdown(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                string schedulerName = SchedulerName;
                await GetRemoteScheduler().Shutdown(cancellationToken).ConfigureAwait(false);
                SchedulerRepository.Instance.Remove(schedulerName);
            }
#if REMOTING
            catch (RemotingException re)
#else // REMOTING
            catch (Exception re) // TODO (NetCore Port): Determine the correct exception type
#endif // REMOTING
            {
                throw InvalidateHandleCreateException("Error communicating with remote scheduler.", re);
            }
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task Shutdown(
            bool waitForJobsToComplete, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.Shutdown(waitForJobsToComplete, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task<DateTimeOffset> ScheduleJob(
            IJobDetail jobDetail, 
            ITrigger trigger,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.ScheduleJob(jobDetail, trigger, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task<DateTimeOffset> ScheduleJob(
            ITrigger trigger,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.ScheduleJob(trigger, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task AddJob(
            IJobDetail jobDetail, 
            bool replace,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.AddJob(jobDetail, replace, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task AddJob(
            IJobDetail jobDetail, 
            bool replace, 
            bool storeNonDurableWhileAwaitingScheduling,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.AddJob(jobDetail, replace, storeNonDurableWhileAwaitingScheduling, cancellationToken));
        }

        public virtual Task<bool> DeleteJobs(
            IList<JobKey> jobKeys, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.DeleteJobs(jobKeys, cancellationToken));
        }

        public virtual Task ScheduleJobs(
            IDictionary<IJobDetail, ISet<ITrigger>> triggersAndJobs, 
            bool replace,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.ScheduleJobs(triggersAndJobs, replace, cancellationToken));
        }

        public Task ScheduleJob(
            IJobDetail jobDetail, 
            ISet<ITrigger> triggersForJob, 
            bool replace,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.ScheduleJob(jobDetail, triggersForJob, replace, cancellationToken));
        }

        public virtual Task<bool> UnscheduleJobs(
            IList<TriggerKey> triggerKeys,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.UnscheduleJobs(triggerKeys, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task<bool> DeleteJob(
            JobKey jobKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.DeleteJob(jobKey, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task<bool> UnscheduleJob(
            TriggerKey triggerKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.UnscheduleJob(triggerKey, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task<DateTimeOffset?> RescheduleJob(
            TriggerKey triggerKey, 
            ITrigger newTrigger,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.RescheduleJob(triggerKey, newTrigger, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task TriggerJob(
            JobKey jobKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return TriggerJob(jobKey, null, cancellationToken);
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task TriggerJob(
            JobKey jobKey,
            JobDataMap data,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.TriggerJob(jobKey, data, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task PauseTrigger(
            TriggerKey triggerKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.PauseTrigger(triggerKey, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task PauseTriggers(
            GroupMatcher<TriggerKey> matcher,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.PauseTriggers(matcher, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task PauseJob(
            JobKey jobKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.PauseJob(jobKey, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task PauseJobs(
            GroupMatcher<JobKey> matcher,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.PauseJobs(matcher, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task ResumeTrigger(
            TriggerKey triggerKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.ResumeTrigger(triggerKey, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task ResumeTriggers(
            GroupMatcher<TriggerKey> matcher,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.ResumeTriggers(matcher, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task ResumeJob(
            JobKey jobKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.ResumeJob(jobKey, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task ResumeJobs(
            GroupMatcher<JobKey> matcher, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.ResumeJobs(matcher, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task PauseAll(CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.PauseAll(cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task ResumeAll(CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.ResumeAll(cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task<ISet<JobKey>> GetJobKeys(
            GroupMatcher<JobKey> matcher,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.GetJobKeys(matcher, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task<IReadOnlyList<ITrigger>> GetTriggersOfJob(
            JobKey jobKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.GetTriggersOfJob(jobKey, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task<ISet<TriggerKey>> GetTriggerKeys(
            GroupMatcher<TriggerKey> matcher,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.GetTriggerKeys(matcher, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task<IJobDetail> GetJobDetail(
            JobKey jobKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.GetJobDetail(jobKey, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task<bool> CheckExists(
            JobKey jobKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.CheckExists(jobKey, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task<bool> CheckExists(
            TriggerKey triggerKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.CheckExists(triggerKey, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task Clear(CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.Clear(cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task<ITrigger> GetTrigger(
            TriggerKey triggerKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.GetTrigger(triggerKey, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task<TriggerState> GetTriggerState(
            TriggerKey triggerKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.GetTriggerState(triggerKey, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task AddCalendar(
            string calName, 
            ICalendar calendar, 
            bool replace, 
            bool updateTriggers,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.AddCalendar(calName, calendar, replace, updateTriggers, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task<bool> DeleteCalendar(
            string calName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.DeleteCalendar(calName, cancellationToken));
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual Task<ICalendar> GetCalendar(
            string calName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.GetCalendar(calName, cancellationToken));
        }

        /// <summary>
        /// Get the names of all registered <see cref="ICalendar"/>.
        /// </summary>
        /// <returns></returns>
        public Task<IReadOnlyList<string>> GetCalendarNames(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CallInGuard(x => x.GetCalendarNames(cancellationToken));
        }

        public IListenerManager ListenerManager
        {
            get { throw new SchedulerException("Operation not supported for remote schedulers."); }
        }

        /// <summary>
        /// Calls the equivalent method on the 'proxied' <see cref="QuartzScheduler" />.
        /// </summary>
        public virtual async Task<bool> Interrupt(
            JobKey jobKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await GetRemoteScheduler().Interrupt(jobKey, cancellationToken).ConfigureAwait(false);
            }
            catch (SchedulerException se)
            {
                throw new UnableToInterruptJobException(se);
            }
#if REMOTING
            catch (RemotingException re)
#else // REMOTING
            catch (Exception re) // TODO (NetCore Port): Determine the correct exception type
#endif // REMOTING
            {
                throw new UnableToInterruptJobException(InvalidateHandleCreateException("Error communicating with remote scheduler.", re));
            }
        }

        public async Task<bool> Interrupt(
            string fireInstanceId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await GetRemoteScheduler().Interrupt(fireInstanceId, cancellationToken).ConfigureAwait(false);
            }
            catch (SchedulerException se)
            {
                throw new UnableToInterruptJobException(se);
            }
#if REMOTING
            catch (RemotingException re)
#else // REMOTING
            catch (Exception re) // TODO (NetCore Port): Determine the correct exception type
#endif // REMOTING
            {
                throw new UnableToInterruptJobException(InvalidateHandleCreateException("Error communicating with remote scheduler.", re));
            }
        }

        protected virtual async Task CallInGuard(Func<IRemotableQuartzScheduler, Task> action)
        {
            try
            {
                await action(GetRemoteScheduler()).ConfigureAwait(false);
            }
#if REMOTING
            catch (RemotingException re)
#else // REMOTING
            catch (Exception re) // TODO (NetCore Port): Determine the correct exception type
#endif // REMOTING
            {
                throw InvalidateHandleCreateException("Error communicating with remote scheduler.", re);
            }
        }

        protected virtual async Task<T> CallInGuard<T>(Func<IRemotableQuartzScheduler, Task<T>> func)
        {
            try
            {
                return await func(GetRemoteScheduler()).ConfigureAwait(false);
            }
#if REMOTING
            catch (RemotingException re)
#else // REMOTING
            catch (Exception re) // TODO (NetCore Port): Determine the correct exception type
#endif // REMOTING
            {
                throw InvalidateHandleCreateException("Error communicating with remote scheduler.", re);
            }
        }

        protected virtual T ReadPropertyInGuard<T>(Func<IRemotableQuartzScheduler, T> action)
        {
            try
            {
                return action(GetRemoteScheduler());
            }
#if REMOTING
            catch (RemotingException re)
#else // REMOTING
            catch (Exception re) // TODO (NetCore Port): Determine the correct exception type
#endif // REMOTING
            {
                throw InvalidateHandleCreateException("Error communicating with remote scheduler.", re);
            }
        }

        protected virtual IRemotableQuartzScheduler GetRemoteScheduler()
        {
            if (rsched != null)
            {
                return rsched;
            }

            try
            {
                rsched = proxyFactory.GetProxy();
            }
            catch (Exception e)
            {
                string errorMessage = $"Could not get handle to remote scheduler: {e.Message}";
                SchedulerException initException = new SchedulerException(errorMessage, e);
                throw initException;
            }

            return rsched;
        }

        protected virtual SchedulerException InvalidateHandleCreateException(string msg, Exception cause)
        {
            rsched = null;
            SchedulerException ex = new SchedulerException(msg, cause);
            return ex;
        }

        public void Dispose()
        {
        }
    }
}