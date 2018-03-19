﻿using System;
using System.Threading;
using System.Runtime.InteropServices;
using GitHub.Helpers;
using GitHub.Services;
using GitHub.VisualStudio;
using GitHub.InlineReviews.Services;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace GitHub.InlineReviews
{
    [Guid(Guids.PullRequestStatusPackageId)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(Guids.UIContext_Git, PackageAutoLoadFlags.BackgroundLoad)]
    public class PullRequestStatusBarPackage : AsyncPackage
    {
        /// <summary>
        /// Initialize the PR status UI on Visual Studio's status bar.
        /// </summary>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            var usageTracker = (IUsageTracker)await GetServiceAsync(typeof(IUsageTracker));
            var serviceProvider = (IGitHubServiceProvider)await GetServiceAsync(typeof(IGitHubServiceProvider));
            var barManager = new PullRequestStatusBarManager(usageTracker, serviceProvider);

            await ThreadingHelper.SwitchToMainThreadAsync();
            barManager.StartShowingStatus();
        }
    }
}
