using Sitecore.Analytics;
using Sitecore.Analytics.Core;
using Sitecore.Analytics.Model;
using Sitecore.Analytics.Rules.Conditions;
using Sitecore.Analytics.Tracking;
using Sitecore.Diagnostics;
using Sitecore.Rules;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sitecore.Support.Analytics.Rules.Conditions
{
    public class GoalWasTriggeredDuringPastOrCurrentInteractionCondition<T> : HasEventOccurredCondition<T> where T : RuleContext
    {
        private Guid? goalGuid;
        private bool goalGuidInitialized;

        public string GoalId { get; set; }

        private Guid? GoalGuid
        {
            get
            {
                if (this.goalGuidInitialized)
                    return this.goalGuid;
                try
                {
                    this.goalGuid = new Guid?(new Guid(this.GoalId));
                }
                catch
                {
                    Log.Warn(string.Format("Could not convert value to guid: {0}", (object)this.GoalId), (object)this.GetType());
                }
                this.goalGuidInitialized = true;
                return this.goalGuid;
            }
        }

        public GoalWasTriggeredDuringPastOrCurrentInteractionCondition()
            : base(false)
        {
        }

        protected GoalWasTriggeredDuringPastOrCurrentInteractionCondition(bool filterByCustomData)
            : base(filterByCustomData)
        {
        }

        protected override bool Execute(T ruleContext)
        {
            Assert.ArgumentNotNull((object)ruleContext, "ruleContext");
            Assert.IsNotNull((object)Tracker.Current, "Tracker.Current is not initialized");
            Assert.IsNotNull((object)Tracker.Current.Session, "Tracker.Current.Session is not initialized");
            Assert.IsNotNull((object)Tracker.Current.Session.Interaction, "Tracker.Current.Session.Interaction is not initialized");
            if (!this.GoalGuid.HasValue)
                return false;
            if (Tracker.Current.Session.Interaction != null && this.HasEventOccurredInInteraction((IInteractionData)Tracker.Current.Session.Interaction))
                return true;
            Assert.IsNotNull((object)Tracker.Current.Contact, "Tracker.Current.Contact is not initialized");
            if (Tracker.Current.Interaction == null)
                Tracker.Current.Contact.LoadKeyBehaviorCache();
            return this.FilterKeyBehaviorCacheEntries(Tracker.Current.Contact.GetKeyBehaviorCache()).Any<KeyBehaviorCacheEntry>((Func<KeyBehaviorCacheEntry, bool>)(entry =>
            {
                Guid id = entry.Id;
                Guid? goalGuid = this.GoalGuid;
                if (!goalGuid.HasValue)
                    return false;
                return id == goalGuid.GetValueOrDefault();
            }));
        }

        protected override IEnumerable<KeyBehaviorCacheEntry> GetKeyBehaviorCacheEntries(KeyBehaviorCache keyBehaviorCache)
        {
            Assert.ArgumentNotNull((object)keyBehaviorCache, "keyBehaviorCache");
            return (IEnumerable<KeyBehaviorCacheEntry>)keyBehaviorCache.Goals;
        }

        protected override bool HasEventOccurredInInteraction(IInteractionData interaction)
        {
            Assert.ArgumentNotNull((object)interaction, "interaction");
            Assert.IsNotNull((object)interaction.Pages, "interaction.Pages is not initialized.");
            return ((IEnumerable<Page>)interaction.Pages).SelectMany<Page, PageEventData>((Func<Page, IEnumerable<PageEventData>>)(page => page.PageEvents)).Any<PageEventData>((Func<PageEventData, bool>)(pageEvent =>
            {
                if (!pageEvent.IsGoal)
                    return false;
                Guid eventDefinitionId = pageEvent.PageEventDefinitionId;
                Guid? goalGuid = this.GoalGuid;
                if (!goalGuid.HasValue)
                    return false;
                return eventDefinitionId == goalGuid.GetValueOrDefault();
            }));
        }
    }
}
