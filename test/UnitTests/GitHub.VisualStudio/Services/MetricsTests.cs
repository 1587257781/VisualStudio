﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using GitHub;
using GitHub.Extensions;
using GitHub.Models;
using GitHub.Services;
using GitHub.Settings;
using NSubstitute;
using NUnit.Framework;
using Rothko;
using UnitTests;
using Environment = System.Environment;

namespace MetricsTests
{
    public class UsageTrackerTests : TestBaseClass
    {
        [Test]
        public void ShouldStartTimer()
        {
            var serviceProvider = CreateServiceProvider();
            var usageService = serviceProvider.TryGetServiceSync<IUsageService>();
            var target = new UsageTracker(serviceProvider, usageService,
                serviceProvider.TryGetServiceSync<IPackageSettings>());

            usageService.Received(1).StartTimer(Arg.Any<Func<Task>>(), TimeSpan.FromMinutes(3), TimeSpan.FromHours(8));
        }

        [Test]
        public async Task FirstTickShouldIncrementLaunchCount()
        {
            var service = CreateUsageService(new UsageModel
            {
                Dimensions = new UsageModel.DimensionsModel
                {
                    Date = DateTimeOffset.Now
                },
                Measures = new UsageModel.MeasuresModel()
            });
            var targetAndTick = CreateTargetAndGetTick(CreateServiceProvider(service), service);

            await targetAndTick.Item2();

            await service.Received(1).WriteLocalData(Arg.Any<UsageData>());
        }

        [Test]
        public async Task SubsequentTickShouldNotIncrementLaunchCount()
        {
            var service = CreateUsageService(new UsageModel
            {
                Dimensions = new UsageModel.DimensionsModel
                {
                    Date = DateTimeOffset.Now
                },
                Measures = new UsageModel.MeasuresModel()
            });
            var targetAndTick = CreateTargetAndGetTick(CreateServiceProvider(service), service);

            await targetAndTick.Item2();
            service.ClearReceivedCalls();
            await targetAndTick.Item2();

            await service.DidNotReceiveWithAnyArgs().WriteLocalData(null);
        }

        [Test]
        public async Task ShouldDisposeTimerIfMetricsServiceNotFound()
        {
            var service = CreateUsageService(new UsageModel
            {
                Dimensions = new UsageModel.DimensionsModel
                {
                    Date = DateTimeOffset.Now
                },
                Measures = new UsageModel.MeasuresModel()
            });
            var disposed = false;
            var disposable = Disposable.Create(() => disposed = true);
            service.StartTimer(null, new TimeSpan(), new TimeSpan()).ReturnsForAnyArgs(disposable);

            var targetAndTick = CreateTargetAndGetTick(
                CreateServiceProvider(service, hasMetricsService: false),
                service);

            await targetAndTick.Item2();

            Assert.True(disposed);
        }

        [Test]
        public async Task TickShouldNotSendDataIfSameDay()
        {
            var usageService = CreateUsageService(new UsageModel
            {
                Dimensions = new UsageModel.DimensionsModel
                {
                    Date = DateTimeOffset.Now
                },
                Measures = new UsageModel.MeasuresModel()
            });
            var serviceProvider = CreateServiceProvider(usageService);
            var targetAndTick = CreateTargetAndGetTick(serviceProvider, usageService);

            await targetAndTick.Item2();

            var metricsService = serviceProvider.TryGetMEFComponent<IMetricsService>();
            await metricsService.DidNotReceive().PostUsage(Arg.Any<UsageModel>());
        }

        [Test]
        public async Task TickShouldSendDataIfDifferentDay()
        {
            var usageService = CreateUsageService(new UsageModel
            {
                Dimensions = new UsageModel.DimensionsModel
                {
                    Date = DateTimeOffset.Now.AddDays(-2)
                },
                Measures = new UsageModel.MeasuresModel()
            });

            var serviceProvider = CreateServiceProvider(usageService);
            var targetAndTick = CreateTargetAndGetTick(serviceProvider, usageService);

            await targetAndTick.Item2();

            var metricsService = serviceProvider.TryGetMEFComponent<IMetricsService>();
            await metricsService.Received(1).PostUsage(Arg.Any<UsageModel>());
        }

        [Test]
        public async Task ShouldIncrementCounter()
        {
            var model = new UsageModel {
                Dimensions = new UsageModel.DimensionsModel {
                    Date = DateTimeOffset.Now
                },
                Measures = new UsageModel.MeasuresModel
                {
                    NumberOfClones = 4
                }
            };
            var usageService = CreateUsageService(model);
            var serviceProvider = CreateServiceProvider(usageService);
            var target = new UsageTracker(serviceProvider, usageService,
                serviceProvider.TryGetServiceSync<IPackageSettings>());

            await target.IncrementCounter(x => x.NumberOfClones);
            UsageData result = usageService.ReceivedCalls().First(x => x.GetMethodInfo().Name == "WriteLocalData").GetArguments()[0] as UsageData;

            Assert.AreEqual(5, result.Reports[0].Measures.NumberOfClones);
        }

        [Test]
        public async Task ShouldWriteData()
        {
            var usageService = CreateUsageService();
            var serviceProvider = CreateServiceProvider(usageService);

            var target = new UsageTracker(
                serviceProvider, usageService,
                serviceProvider.TryGetServiceSync<IPackageSettings>());

            await target.IncrementCounter(x => x.NumberOfClones);
            await usageService.Received(1).WriteLocalData(Arg.Is<UsageData>(data => 
                data.Reports.Count == 1 &&
                data.Reports[0].Dimensions.Date.Date == DateTimeOffset.Now.Date &&
                data.Reports[0].Dimensions.AppVersion == AssemblyVersionInformation.Version &&
                data.Reports[0].Dimensions.Lang == CultureInfo.InstalledUICulture.IetfLanguageTag &&
                data.Reports[0].Dimensions.CurrentLang == CultureInfo.CurrentCulture.IetfLanguageTag &&
                data.Reports[0].Measures.NumberOfClones == 1
                ));
        }

        [Test]
        public async Task ShouldWriteUpdatedData()
        {
            var date = DateTimeOffset.Now;
            var usageService = CreateUsageService(new UsageModel
            {
                Dimensions = new UsageModel.DimensionsModel
                {
                    AppVersion = AssemblyVersionInformation.Version,
                    Lang = CultureInfo.InstalledUICulture.IetfLanguageTag,
                    CurrentLang = CultureInfo.CurrentCulture.IetfLanguageTag,
                    Date = date
                },
                Measures = new UsageModel.MeasuresModel
                {
                    NumberOfClones = 1
                }
            });
            var serviceProvider = CreateServiceProvider(usageService);

            var target = new UsageTracker(
                serviceProvider, usageService,
                serviceProvider.TryGetServiceSync<IPackageSettings>());

            await target.IncrementCounter(x => x.NumberOfClones);
            await usageService.Received(1).WriteLocalData(Arg.Is<UsageData>(data =>
                data.Reports.Count == 1 &&
                data.Reports[0].Dimensions.Date.Date == DateTimeOffset.Now.Date &&
                data.Reports[0].Dimensions.AppVersion == AssemblyVersionInformation.Version &&
                data.Reports[0].Dimensions.Lang == CultureInfo.InstalledUICulture.IetfLanguageTag &&
                data.Reports[0].Dimensions.CurrentLang == CultureInfo.CurrentCulture.IetfLanguageTag &&
                data.Reports[0].Measures.NumberOfClones == 2
            ));
        }

        static Tuple<UsageTracker, Func<Task>> CreateTargetAndGetTick(
            IGitHubServiceProvider serviceProvider,
            IUsageService usageService)
        {
            Func<Task> tick = null;
            usageService.WhenForAnyArgs(x => x.StartTimer(null, new TimeSpan(), new TimeSpan()))
                .Do(x => tick = x.ArgAt<Func<Task>>(0));

            var target = new UsageTracker(serviceProvider,
                usageService,
                serviceProvider.TryGetServiceSync<IPackageSettings>());

            return Tuple.Create(target, tick);
        }

        static IGitHubServiceProvider CreateServiceProvider(
            IUsageService usageService = null,
            bool hasMetricsService = true)
        {
            var result = Substitutes.ServiceProvider;

            var metricsService = hasMetricsService ? Substitute.For<IMetricsService>() : null;
            result.SetupMEF(metricsService);
            result.SetupService(usageService);

            var connectionManager = result.TryGetMEFComponent<IConnectionManager>();
            connectionManager.Connections.Returns(new ObservableCollectionEx<IConnection>());

            var packageSettings = result.TryGetServiceSync<IPackageSettings>();
            packageSettings.CollectMetrics.Returns(true);

            return result;
        }

        static IUsageService CreateUsageService(
            UsageModel model = null)
        {
            return CreateUsageService(new UsageData
            {
                Reports = model != null ? new List<UsageModel>{ model } : new List<UsageModel>()
            });
        }

        static IUsageService CreateUsageService(UsageData data)
        {
            var result = Substitute.For<IUsageService>();
            result.ReadLocalData().Returns(data);
            return result;
        }
    }

    public class UsageServiceTests : TestBaseClass
    {
        static readonly Guid UserGuid = Guid.NewGuid();
        static readonly string DefaultUserStoreContent = @"{""UserGuid"":""" + UserGuid + @"""}";

        static readonly string DefaultUsageContent = @"{""Reports"":[{""Dimensions"":{""Guid"":""26fa0c25-653f-4fa5-ad83-7438ad526b0a"",""Date"":""2018-03-13T18:45:19.0453424Z"",""IsGitHubUser"":false,""IsEnterpriseUser"":false,""AppVersion"":null,""VSVersion"":null,""Lang"":null,""CurrentLang"":null},""Measures"":{""NumberOfStartups"":0,""NumberOfUpstreamPullRequests"":0,""NumberOfClones"":1,""NumberOfReposCreated"":0,""NumberOfReposPublished"":2,""NumberOfGists"":0,""NumberOfOpenInGitHub"":0,""NumberOfLinkToGitHub"":0,""NumberOfLogins"":0,""NumberOfOAuthLogins"":0,""NumberOfTokenLogins"":0,""NumberOfPullRequestsOpened"":3,""NumberOfLocalPullRequestsCheckedOut"":0,""NumberOfLocalPullRequestPulls"":0,""NumberOfLocalPullRequestPushes"":0,""NumberOfForkPullRequestsCheckedOut"":0,""NumberOfForkPullRequestPulls"":0,""NumberOfForkPullRequestPushes"":0,""NumberOfSyncSubmodules"":0,""NumberOfWelcomeDocsClicks"":0,""NumberOfWelcomeTrainingClicks"":0,""NumberOfGitHubPaneHelpClicks"":0,""NumberOfPRDetailsViewChanges"":0,""NumberOfPRDetailsViewFile"":0,""NumberOfPRDetailsCompareWithSolution"":0,""NumberOfPRDetailsOpenFileInSolution"":0,""NumberOfPRDetailsNavigateToEditor"":0,""NumberOfPRReviewDiffViewInlineCommentOpen"":0,""NumberOfPRReviewDiffViewInlineCommentPost"":0,""NumberOfShowCurrentPullRequest"":0}}]}";

        string usageFileName;
        string userFileName;
        string localApplicationDataPath;
        IEnvironment environment;

        [SetUp]
        public void SetUp()
        {
            localApplicationDataPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            if (File.Exists(localApplicationDataPath))
            {
                File.Delete(localApplicationDataPath);
            }

            if (Directory.Exists(localApplicationDataPath))
            {
                Directory.Delete(localApplicationDataPath);
            }

            Directory.CreateDirectory(localApplicationDataPath);

            usageFileName = Path.Combine(localApplicationDataPath, "metrics.json");
            userFileName = Path.Combine(localApplicationDataPath, "user.json");

            environment = Substitute.For<IEnvironment>();
            environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                .Returns(localApplicationDataPath);

            WriteUsageFileContent(DefaultUsageContent);
            WriteUserFileContent(DefaultUserStoreContent);
        }

        void WriteUsageFileContent(string content)
        {
            File.WriteAllText(usageFileName, content);
        }

        void WriteUserFileContent(string content)
        {
            File.WriteAllText(userFileName, content);
        }

        [Test]
        public async Task GetUserGuidWorks()
        {
            var usageService = new UsageService(Substitute.For<IGitHubServiceProvider>(), environment);
            var guid = await usageService.GetUserGuid();
            Assert.IsTrue(guid.Equals(UserGuid));
        }

        [Test]
        public async Task GetUserGuidWorksWhenFileMissing()
        {
            File.Delete(userFileName);

            var usageService = new UsageService(Substitute.For<IGitHubServiceProvider>(), environment);
            var guid = await usageService.GetUserGuid();
            Assert.AreNotEqual(guid, Guid.Empty);
        }

        [Test]
        public async Task ReadUsageDataWorks()
        {
            var usageService = new UsageService(Substitute.For<IGitHubServiceProvider>(), environment);
            var usageData = await usageService.ReadLocalData();

            Assert.IsNotNull(usageData);
            Assert.IsNotNull(usageData.Reports);
            Assert.AreEqual(1, usageData.Reports.Count);
            Assert.AreEqual(1, usageData.Reports[0].Measures.NumberOfClones);
            Assert.AreEqual(2, usageData.Reports[0].Measures.NumberOfReposPublished);
            Assert.AreEqual(3, usageData.Reports[0].Measures.NumberOfPullRequestsOpened);
        }

        [Test]
        public async Task ReadUsageDataWorksWhenFileMissing()
        {
            File.Delete(usageFileName);

            var usageService = new UsageService(Substitute.For<IGitHubServiceProvider>(), environment);
            var usageData = await usageService.ReadLocalData();

            Assert.IsNotNull(usageData);
            Assert.IsNotNull(usageData.Reports);
            Assert.AreEqual(0, usageData.Reports.Count);
        }
    }
}
