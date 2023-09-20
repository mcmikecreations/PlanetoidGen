using Microsoft.Extensions.DependencyInjection;
using Moq;
using PlanetoidGen.Agents.Procedural.Agents.Relief;
using PlanetoidGen.Agents.Procedural.Agents.Relief.Models;
using PlanetoidGen.BusinessLogic.Common.Constants;
using PlanetoidGen.BusinessLogic.Common.Helpers;
using PlanetoidGen.Contracts.Models.Documents;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using PlanetoidGen.Contracts.Services.Agents;
using PlanetoidGen.Contracts.Services.Documents;
using PlanetoidGen.Domain.Models.Documents;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using Xunit.Abstractions;

namespace PlanetoidGen.Agents.Tests.Unit.Procedural.Relief
{
    public class ReliefAgentTests : BaseAgentTests
    {
        public ReliefAgentTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [Fact]
        public async Task GivenDefaultSettings_WritesSingleTile()
        {
            FileModel? actualFileModel = null;
            ITypedAgent<ReliefAgentSettings> agent = new ReliefAgent();
            var (serviceProviderMock, fileContentServiceMock) = SetupFileContentService(
                (fm, _) =>
                {
                    actualFileModel = fm;
                });
            var job = new GenerationJobMessage
            {
                Id = "id",
                PlanetoidId = 1,
                Z = 0,
                X = 0,
                Y = 0,
                AgentIndex = 1
            };

            var expectedFileId = FileModelFormatter.FormatFileId(
                job.PlanetoidId,
                DataTypes.HeightMapRgba32Encoded,
                job.Z,
                job.X,
                job.Y);
            var expectedFileName = FileModelFormatter.FormatFileName(job.Y, "png");
            var expectedLocalPath = FileModelFormatter.FormatLocalPath(
                job.PlanetoidId,
                DataTypes.HeightMapRgba32Encoded,
                job.Z,
                job.X);
            var expectedTileBasedInfo = new TileBasedFileInfoModel(expectedFileId, job.PlanetoidId, job.Z, job.X, job.Y);

            var settings = agent.GetTypedDefaultSettings();
            var initResult = await agent.Initialize(await settings.Serialize(), serviceProviderMock);
            var executionResult = await agent.Execute(job, CancellationToken.None);

            Assert.True(initResult.Success, initResult.ErrorMessage?.ToString());
            Assert.True(executionResult.Success, executionResult.ErrorMessage?.ToString());

            fileContentServiceMock.Verify(
                x => x.SaveFileContentWithDependencies(It.IsAny<FileModel>(), CancellationToken.None),
                Times.Once());

            Assert.NotNull(actualFileModel);
            Assert.Equal(expectedFileId, actualFileModel?.FileId);
            Assert.NotNull(actualFileModel?.Content);
            Assert.Equal(expectedFileId, actualFileModel?.Content.Id);
            Assert.Equal(expectedFileName, actualFileModel!.Content.FileName);
            Assert.Equal(expectedLocalPath, actualFileModel!.Content.LocalPath);
            Assert.NotNull(actualFileModel?.Content?.Content);

            using (var image = Image.Load<Rgba32>(actualFileModel!.Content.Content))
            {
                Assert.Equal(settings.TileSizeInPixels, image.Height);
                Assert.Equal(settings.TileSizeInPixels, image.Width);
            }

            Assert.NotNull(actualFileModel?.TileBasedFileInfo);
            Assert.Equivalent(expectedTileBasedInfo, actualFileModel!.TileBasedFileInfo);
        }

        [Fact]
        public async Task GivenDefaultSettings_WritesMultipleTiles()
        {
            var planetoidId = 1;
            IAgent agent = new ReliefAgent();
            var (provider, fileContentServiceMock) = SetupFileContentService();

            var jobs = new GenerationJobMessage[]
            {
                new GenerationJobMessage
                {
                    Id = "id",
                    PlanetoidId = planetoidId,
                    Z = 0,
                    X = 0,
                    Y = 0,
                    AgentIndex = 1
                },
                new GenerationJobMessage
                {
                    Id = "id",
                    PlanetoidId = planetoidId,
                    Z = 1,
                    X = 0,
                    Y = 0,
                    AgentIndex = 1
                },
                new GenerationJobMessage
                {
                    Id = "id",
                    PlanetoidId = planetoidId,
                    Z = 1,
                    X = 1,
                    Y = 0,
                    AgentIndex = 1
                },
                new GenerationJobMessage
                {
                    Id = "id",
                    PlanetoidId = planetoidId,
                    Z = 1,
                    X = 0,
                    Y = 6,
                    AgentIndex = 1
                },
                new GenerationJobMessage
                {
                    Id = "id",
                    PlanetoidId = planetoidId,
                    Z = 1,
                    X = 1,
                    Y = 6,
                    AgentIndex = 1
                },
            };

            var initResult = await agent.Initialize(await agent.GetDefaultSettings(), provider);
            var executionResults = jobs
                .Select(async j => await agent.Execute(j, CancellationToken.None))
                .Select(j => j.Result)
                .ToList();

            Assert.True(initResult.Success, initResult.ErrorMessage?.ToString());
            Assert.All(executionResults, x => Assert.True(x.Success, x.ErrorMessage?.ToString()));

            fileContentServiceMock.Verify(
                x => x.SaveFileContentWithDependencies(It.IsAny<FileModel>(), CancellationToken.None),
                Times.Exactly(jobs.Length));
        }

        [Fact]
        public async Task GivenDefaultSettings_WritesAllTileForZeroZoom()
        {
            var planetoidId = 1;
            short zoom = 0;
            IAgent agent = new ReliefAgent();
            var (provider, fileContentServiceMock) = SetupFileContentService();

            var jobs = new GenerationJobMessage[]
            {
                new GenerationJobMessage
                {
                    Id = "id",
                    PlanetoidId = planetoidId,
                    Z = zoom,
                    X = 0,
                    Y = 0,
                    AgentIndex = 1
                },
                new GenerationJobMessage
                {
                    Id = "id",
                    PlanetoidId = planetoidId,
                    Z = zoom,
                    X = 0,
                    Y = 1,
                    AgentIndex = 1
                },
                new GenerationJobMessage
                {
                    Id = "id",
                    PlanetoidId = planetoidId,
                    Z = zoom,
                    X = 0,
                    Y = 2,
                    AgentIndex = 1
                },
                new GenerationJobMessage
                {
                    Id = "id",
                    PlanetoidId = planetoidId,
                    Z = zoom,
                    X = 0,
                    Y = 3,
                    AgentIndex = 1
                },
                new GenerationJobMessage
                {
                    Id = "id",
                    PlanetoidId = planetoidId,
                    Z = zoom,
                    X = 0,
                    Y = 4,
                    AgentIndex = 1
                },
                new GenerationJobMessage
                {
                    Id = "id",
                    PlanetoidId = planetoidId,
                    Z = zoom,
                    X = 0,
                    Y = 5,
                    AgentIndex = 1
                },
            };

            var initResult = await agent.Initialize(await agent.GetDefaultSettings(), provider);
            var executionResults = jobs
                .Select(async j => await agent.Execute(j, CancellationToken.None))
                .Select(j => j.Result)
                .ToList();

            Assert.True(initResult.Success, initResult.ErrorMessage?.ToString());
            Assert.All(executionResults, x => Assert.True(x.Success, x.ErrorMessage?.ToString()));

            fileContentServiceMock.Verify(
                x => x.SaveFileContentWithDependencies(It.IsAny<FileModel>(), CancellationToken.None),
                Times.Exactly(jobs.Length));
        }

        [Fact]
        public async Task GivenDefaultSettings_WritesRelativeTilesFor7Zoom()
        {
            var planetoidId = 1;
            short zoom = 7;
            IAgent agent = new ReliefAgent();
            var (provider, fileContentServiceMock) = SetupFileContentService();

            var jobs = new GenerationJobMessage[]
            {
                new GenerationJobMessage
                {
                    Id = "id",
                    PlanetoidId = planetoidId,
                    Z = zoom,
                    X = 62,
                    Y = 1 + 6 * 62,
                    AgentIndex = 1
                },
                new GenerationJobMessage
                {
                    Id = "id",
                    PlanetoidId = planetoidId,
                    Z = zoom,
                    X = 63,
                    Y = 1 + 6 * 62,
                    AgentIndex = 1
                },
                new GenerationJobMessage
                {
                    Id = "id",
                    PlanetoidId = planetoidId,
                    Z = zoom,
                    X = 64,
                    Y = 1 + 6 * 62,
                    AgentIndex = 1
                },
                new GenerationJobMessage
                {
                    Id = "id",
                    PlanetoidId = planetoidId,
                    Z = zoom,
                    X = 62,
                    Y = 1 + 6 * 63,
                    AgentIndex = 1
                },
                new GenerationJobMessage
                {
                    Id = "id",
                    PlanetoidId = planetoidId,
                    Z = zoom,
                    X = 63,
                    Y = 1 + 6 * 63,
                    AgentIndex = 1
                },
                new GenerationJobMessage
                {
                    Id = "id",
                    PlanetoidId = planetoidId,
                    Z = zoom,
                    X = 64,
                    Y = 1 + 6 * 63,
                    AgentIndex = 1
                },
                new GenerationJobMessage
                {
                    Id = "id",
                    PlanetoidId = planetoidId,
                    Z = zoom,
                    X = 62,
                    Y = 1 + 6 * 64,
                    AgentIndex = 1
                },
                new GenerationJobMessage
                {
                    Id = "id",
                    PlanetoidId = planetoidId,
                    Z = zoom,
                    X = 63,
                    Y = 1 + 6 * 64,
                    AgentIndex = 1
                },
                new GenerationJobMessage
                {
                    Id = "id",
                    PlanetoidId = planetoidId,
                    Z = zoom,
                    X = 64,
                    Y = 1 + 6 * 64,
                    AgentIndex = 1
                },
            };

            var initResult = await agent.Initialize(await agent.GetDefaultSettings(), provider);
            var executionResults = jobs
                .Select(async j => await agent.Execute(j, CancellationToken.None))
                .Select(j => j.Result)
                .ToList();

            Assert.True(initResult.Success, initResult.ErrorMessage?.ToString());
            Assert.All(executionResults, x => Assert.True(x.Success, x.ErrorMessage?.ToString()));

            fileContentServiceMock.Verify(
                x => x.SaveFileContentWithDependencies(It.IsAny<FileModel>(), CancellationToken.None),
                Times.Exactly(jobs.Length));
        }

        private (IServiceProvider provider, Mock<IFileContentService> fileContentServiceMock) SetupFileContentService(
            Action<FileModel, CancellationToken>? callback = null)
        {
            var services = base.SetupServices();
            var fileContentServiceMock = new Mock<IFileContentService>();

            if (callback != null)
            {
                fileContentServiceMock
                    .Setup(x => x.SaveFileContentWithDependencies(It.IsAny<FileModel>(), CancellationToken.None))
                    .Callback(callback)
                    .ReturnsAsync(Result<bool>.CreateSuccess());
            }
            else
            {
                fileContentServiceMock
                    .Setup(x => x.SaveFileContentWithDependencies(It.IsAny<FileModel>(), CancellationToken.None))
                    .ReturnsAsync(Result<bool>.CreateSuccess());
            }

            services.AddSingleton(fileContentServiceMock.Object);

            return (services.BuildServiceProvider(), fileContentServiceMock);
        }
    }
}
