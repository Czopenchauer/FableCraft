using FableCraft.Tests.Integration.Docker.Fixtures;
using FableCraft.Tests.Integration.Docker.Helpers;

namespace FableCraft.Tests.Integration.Docker;

[ClassDataSource<DockerFixture>(Shared = SharedType.PerTestSession)]
public class VolumeManagerTests(DockerFixture fixture)
{
    [Test]
    public async Task CreateAsync_CreatesVolumeWithFableCraftLabels()
    {
        var volumeName = fixture.CreateTestVolumeName();

        await fixture.VolumeManager.CreateAsync(volumeName);

        var info = await fixture.Client.Volumes.InspectAsync(volumeName);
        await Assert.That(info.Labels["fablecraft.managed"]).IsEqualTo("true");
        await Assert.That(info.Labels.ContainsKey("fablecraft.created")).IsTrue();
    }

    [Test]
    public async Task ExistsAsync_ReturnsTrueForExisting_FalseForMissing()
    {
        var existingVolume = fixture.CreateTestVolumeName();
        var missingVolume = $"fablecraft-test-missing-{Guid.NewGuid():N}";

        await fixture.VolumeManager.CreateAsync(existingVolume);

        await Assert.That(await fixture.VolumeManager.ExistsAsync(existingVolume)).IsTrue();
        await Assert.That(await fixture.VolumeManager.ExistsAsync(missingVolume)).IsFalse();
    }

    [Test]
    public async Task DeleteAsync_RemovesVolume_IsIdempotent()
    {
        var volumeName = fixture.CreateTestVolumeName();
        await fixture.VolumeManager.CreateAsync(volumeName);

        await fixture.VolumeManager.DeleteAsync(volumeName);

        await Assert.That(await fixture.VolumeManager.ExistsAsync(volumeName)).IsFalse();

        // Second delete should not throw
        await fixture.VolumeManager.DeleteAsync(volumeName);
    }

    [Test]
    public async Task CopyAsync_DuplicatesVolumeContents()
    {
        var source = fixture.CreateTestVolumeName();
        var dest = fixture.CreateTestVolumeName();
        var markerContent = Guid.NewGuid().ToString();

        await fixture.VolumeManager.CreateAsync(source);
        await VolumeTestHelpers.WriteFileToVolume(
            fixture.Client,
            fixture.UtilityImage,
            source,
            "test/marker.txt",
            markerContent);

        await fixture.VolumeManager.CopyAsync(source, dest);

        var readContent = await VolumeTestHelpers.ReadFileFromVolume(
            fixture.Client,
            fixture.UtilityImage,
            dest,
            "test/marker.txt");

        await Assert.That(readContent).IsEqualTo(markerContent);
    }

    [Test]
    public async Task ExportImportAsync_RoundTripsVolumeData()
    {
        var sourceVolume = fixture.CreateTestVolumeName();
        var destVolume = fixture.CreateTestVolumeName();
        var tempDir = Path.Combine(Path.GetTempPath(), $"fablecraft-test-{Guid.NewGuid():N}");
        var markerContent = Guid.NewGuid().ToString();

        try
        {
            Directory.CreateDirectory(tempDir);

            await fixture.VolumeManager.CreateAsync(sourceVolume);
            await VolumeTestHelpers.WriteFileToVolume(
                fixture.Client,
                fixture.UtilityImage,
                sourceVolume,
                "data.txt",
                markerContent);

            var tarPath = await fixture.VolumeManager.ExportAsync(sourceVolume, tempDir);

            await fixture.VolumeManager.ImportAsync(tarPath, destVolume);

            var readContent = await VolumeTestHelpers.ReadFileFromVolume(
                fixture.Client,
                fixture.UtilityImage,
                destVolume,
                "data.txt");

            await Assert.That(readContent).IsEqualTo(markerContent);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }
}
