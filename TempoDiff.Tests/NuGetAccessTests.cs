using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tempo;

namespace Tempo.Tests
{
    [TestClass]
    public class NuGetAccessTests
    {
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
