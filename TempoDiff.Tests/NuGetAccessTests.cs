using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tempo;

namespace Tempo.Tests
{
    [TestClass]
    public class NuGetAccessTests
    {
        [TestMethod]
        [DoNotParallelize]
        public async Task ConfiguredFeedIsUsedForMetadataAndPackageContent()
        {
            var portReservation = new TcpListener(IPAddress.Loopback, 0);
            portReservation.Start();
            var port = ((IPEndPoint)portReservation.LocalEndpoint).Port;
            portReservation.Stop();

            var feed = $"http://localhost:{port}/";
            var packageId = "TempoProxyTest" + Guid.NewGuid().ToString("N");
            var packagePath = $"flat/{packageId.ToLowerInvariant()}/1.0.0/{packageId.ToLowerInvariant()}.1.0.0.nupkg";
            var registrationPath = $"registration/{packageId.ToLowerInvariant()}/index.json";
            var cachePath = Path.Combine(Path.GetTempPath(), packageId);
            var originalFeed = DesktopManager2.NuGetFeedUri;
            var originalCache = DesktopManager2.PackagesCachePath;
            var requests = new List<string>();
            using var package = new MemoryStream();
            using (var zip = new ZipArchive(package, ZipArchiveMode.Create, leaveOpen: true))
            using (var writer = new StreamWriter(zip.CreateEntry(packageId + ".nuspec").Open()))
            {
                writer.Write($"<package><metadata><id>{packageId}</id><version>1.0.0</version><authors>Tempo</authors><description>Proxy test</description></metadata></package>");
            }

            using var listener = new HttpListener();
            listener.Prefixes.Add(feed);
            listener.Start();
            using var cancellation = new CancellationTokenSource();
            var server = Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        var context = await listener.GetContextAsync().WaitAsync(cancellation.Token);
                        var path = context.Request.Url!.AbsolutePath.TrimStart('/');
                        requests.Add(path);
                        string? json = path switch
                        {
                            "index.json" => $$"""
                                {"version":"3.0.0","resources":[
                                  {"@id":"{{feed}}registration/","@type":"RegistrationsBaseUrl/3.6.0"},
                                  {"@id":"{{feed}}flat/","@type":"PackageBaseAddress/3.0.0"}]}
                                """,
                            _ when path == $"flat/{packageId.ToLowerInvariant()}/index.json" =>
                                """{"versions":["1.0.0"]}""",
                            _ when path == registrationPath => $$"""
                                {"@id":"{{feed}}{{registrationPath}}","count":1,"items":[
                                  {"@id":"{{feed}}registration/page.json","count":1,"lower":"1.0.0","upper":"1.0.0","items":[
                                    {"@id":"{{feed}}registration/1.0.0.json","catalogEntry":{
                                      "@id":"{{feed}}catalog/1.0.0.json","id":"{{packageId}}","version":"1.0.0","listed":true,
                                      "authors":"Tempo","description":"Proxy test"},
                                      "packageContent":"{{feed}}{{packagePath}}"}]}]}
                                """,
                            _ => null
                        };

                        var bytes = path == packagePath ? package.ToArray()
                            : json != null ? Encoding.UTF8.GetBytes(json) : Array.Empty<byte>();
                        context.Response.StatusCode = bytes.Length == 0 ? 404 : 200;
                        context.Response.ContentType = path == packagePath ? "application/octet-stream" : "application/json";
                        context.Response.ContentLength64 = bytes.Length;
                        await context.Response.OutputStream.WriteAsync(bytes);
                        context.Response.Close();
                    }
                }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
                {
                }
            });

            try
            {
                DesktopManager2.NuGetFeedUri = new Uri(feed + "index.json");
                DesktopManager2.PackagesCachePath = cachePath;
                var result = await DesktopManager2.DownloadLatestPackageFromNugetToDirectory(packageId, task: null);
                Assert.AreEqual("1.0.0", result.Version);
                CollectionAssert.AreEqual(package.ToArray(), File.ReadAllBytes(result.PrimaryPath));
            }
            finally
            {
                DesktopManager2.NuGetFeedUri = originalFeed;
                DesktopManager2.PackagesCachePath = originalCache;
                cancellation.Cancel();
                await server;
                if (Directory.Exists(cachePath))
                {
                    Directory.Delete(cachePath, recursive: true);
                }
            }

            CollectionAssert.Contains(requests, "index.json");
            CollectionAssert.Contains(requests, registrationPath);
            CollectionAssert.Contains(requests, packagePath);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("  ")]
        public void EmptyProxyUsesNuGetOrg(string? proxyUri)
        {
            Assert.IsTrue(DesktopManager2.TryGetNuGetFeedUri(proxyUri, out var feedUri));
            Assert.AreEqual(DesktopManager2.DefaultNuGetFeedUrl, feedUri.AbsoluteUri);
        }

        [DataTestMethod]
        [DataRow(" https://proxy.example/nuget/v3/index.json ", "https://proxy.example/nuget/v3/index.json")]
        [DataRow("http://localhost:8080/v3/index.json", "http://localhost:8080/v3/index.json")]
        public void AcceptsAbsoluteHttpFeedUris(string proxyUri, string expectedUri)
        {
            Assert.IsTrue(DesktopManager2.TryGetNuGetFeedUri(proxyUri, out var feedUri));
            Assert.AreEqual(expectedUri, feedUri.AbsoluteUri);
        }

        [DataTestMethod]
        [DataRow("proxy.example/v3/index.json")]
        [DataRow("file:///C:/feed/index.json")]
        [DataRow("ftp://proxy.example/index.json")]
        [DataRow("https://user:password@proxy.example/index.json")]
        [DataRow("https://proxy.example/index.json#fragment")]
        public void RejectsInvalidProxyUris(string proxyUri)
        {
            Assert.IsFalse(DesktopManager2.TryGetNuGetFeedUri(proxyUri, out _));
        }

        [TestMethod]
        public void DetectsWrappedNetworkFailures()
        {
            Exception[] failures =
            {
                new WebException("DNS failure", WebExceptionStatus.NameResolutionFailure),
                new WebException("Blocked", WebExceptionStatus.ConnectFailure),
                new WebException("TLS failure", WebExceptionStatus.SecureChannelFailure),
                new HttpRequestException("Request failed"),
                new AuthenticationException("TLS failure"),
                new SocketException((int)SocketError.AccessDenied),
                new TaskCanceledException("HTTP timeout")
            };

            foreach (var failure in failures)
            {
                Assert.IsTrue(NuGetAccessException.IsAccessFailure(
                    new AggregateException(new InvalidOperationException("NuGet request failed", failure))),
                    failure.GetType().Name);
            }
        }

        [TestMethod]
        public void DoesNotClassifyLocalErrorsOrExplicitCancellationAsBlockedAccess()
        {
            Assert.IsFalse(NuGetAccessException.IsAccessFailure(new IOException("Disk full")));
            Assert.IsFalse(NuGetAccessException.IsAccessFailure(new InvalidDataException("Invalid package")));
            Assert.IsFalse(NuGetAccessException.IsAccessFailure(new OperationCanceledException()));
            Assert.IsFalse(NuGetAccessException.IsAccessFailure(
                new WebException("Canceled", WebExceptionStatus.RequestCanceled)));
        }
    }
}
