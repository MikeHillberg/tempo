using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Tempo
{
    public class NuGetAccessException : Exception
    {
        // netstandard2.0 doesn't define this HttpStatusCode member.
        const HttpStatusCode UnavailableForLegalReasons = (HttpStatusCode)451;

        public NuGetAccessException(Exception innerException)
            : base("Unable to access the NuGet feed.", innerException)
        {
        }

        public static bool IsAccessFailure(Exception exception)
        {
            if (exception is AggregateException aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions)
                {
                    if (IsAccessFailure(inner))
                    {
                        return true;
                    }
                }
            }

            if (exception is WebException webException)
            {
                if (webException.Status == WebExceptionStatus.RequestCanceled)
                {
                    return false;
                }

                if (webException.Response is HttpWebResponse response)
                {
                    var statusCode = response.StatusCode;
                    return statusCode == HttpStatusCode.Unauthorized
                        || statusCode == HttpStatusCode.Forbidden
                        || statusCode == HttpStatusCode.ProxyAuthenticationRequired
                        || statusCode == UnavailableForLegalReasons
                        || statusCode >= HttpStatusCode.InternalServerError;
                }

                return true;
            }

            return exception is HttpRequestException
                || exception is SocketException
                || exception is AuthenticationException
                || exception is TaskCanceledException
                || (exception?.InnerException != null && IsAccessFailure(exception.InnerException));
        }
    }
}
