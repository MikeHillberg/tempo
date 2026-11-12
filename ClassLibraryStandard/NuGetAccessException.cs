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
        public NuGetAccessException(Exception innerException)
            : base("Unable to access nuget.org.", innerException)
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
                    var statusCode = (int)response.StatusCode;
                    return statusCode == 401 || statusCode == 403 || statusCode == 407
                        || statusCode == 451 || statusCode >= 500;
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
