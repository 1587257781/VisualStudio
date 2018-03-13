﻿using System;

namespace GitHub.Models
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes", Justification = "It'll use reflection by default and we're fine with that")]
    public struct UsageModel
    {
        public Guid Guid { get; set; }
        public bool IsGitHubUser { get; set; }
        public bool IsEnterpriseUser { get; set; }
        public string AppVersion { get; set; }
        public string VSVersion { get; set; }
        public string Lang { get; set; }
        public int NumberOfStartups { get; set; }
        public int NumberOfStartupsWeek { get; set; }
        public int NumberOfStartupsMonth { get; set; }
        public int NumberOfUpstreamPullRequests { get; set; }
        public int NumberOfClones { get; set; }
        public int NumberOfReposCreated { get; set; }
        public int NumberOfReposPublished { get; set; }
        public int NumberOfGists { get; set; }
        public int NumberOfOpenInGitHub { get; set; }
        public int NumberOfLinkToGitHub { get; set; }
        public int NumberOfLogins { get; set; }
        public int NumberOfOAuthLogins { get; set; }
        public int NumberOfTokenLogins { get; set; }
        public int NumberOfPullRequestsOpened { get; set; }
        public int NumberOfLocalPullRequestsCheckedOut { get; set; }
        public int NumberOfLocalPullRequestPulls { get; set; }
        public int NumberOfLocalPullRequestPushes { get; set; }
        public int NumberOfForkPullRequestsCheckedOut { get; set; }
        public int NumberOfForkPullRequestPulls { get; set; }
        public int NumberOfForkPullRequestPushes { get; set; }
        public int NumberOfSyncSubmodules { get; set; }
        public int NumberOfWelcomeDocsClicks { get; set; }
        public int NumberOfWelcomeTrainingClicks { get; set; }
        public int NumberOfGitHubPaneHelpClicks { get; set; }
        public int NumberOfPRDetailsViewChanges { get; set; }
        public int NumberOfPRDetailsViewFile { get; set; }
        public int NumberOfPRDetailsCompareWithSolution { get; set; }
        public int NumberOfPRDetailsOpenFileInSolution { get; set; }
        public int NumberOfPRDetailsNavigateToEditor { get; set; }
        public int NumberOfPRReviewDiffViewInlineCommentOpen { get; set; }
        public int NumberOfPRReviewDiffViewInlineCommentPost { get; set; }
        public int NumberOfShowCurrentPullRequest { get; set; }

        public UsageModel Clone(bool includeWeekly, bool includeMonthly)
        {
            var result = this;
            if (!includeWeekly)
                result.NumberOfStartupsWeek = 0;
            if (!includeMonthly)
                result.NumberOfStartupsMonth = 0;
            return result;
        }

        public UsageModel ClearCounters(bool clearWeekly, bool clearMonthly)
        {
            var result = new UsageModel();
            if (!clearWeekly)
                result.NumberOfStartupsWeek = NumberOfStartupsWeek;
            if (!clearMonthly)
                result.NumberOfStartupsMonth = NumberOfStartupsMonth;

            result.IsGitHubUser = IsGitHubUser;
            result.IsEnterpriseUser = IsEnterpriseUser;
            result.AppVersion = AppVersion;
            result.VSVersion = VSVersion;
            result.Lang = Lang;
            return result;
        }
    }
}
