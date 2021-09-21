using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Management.Automation;

namespace ContainerDesktop.Common
{
    public static class PowershellExtensions
    {
        public static IDisposable WriteToLog(this PSDataStreams streams, ILogger logger)
        {
            return new DataStreamsLogger(streams, logger);
        }

        private sealed class DataStreamsLogger : IDisposable
        {
            private ILogger _logger;
            private PSDataStreams _streams;

            public DataStreamsLogger(PSDataStreams streams, ILogger logger)
            {
                _logger = logger;
                _streams = streams;
                streams.Error.DataAdded += DataAdded;
                streams.Warning.DataAdded += DataAdded;
                streams.Verbose.DataAdded += DataAdded;
                streams.Debug.DataAdded += DataAdded;
                streams.Information.DataAdded += DataAdded;
                streams.Progress.DataAdded += DataAdded;
            }

            public void Dispose()
            {
                _streams.Error.DataAdded -= DataAdded;
                _streams.Warning.DataAdded -= DataAdded;
                _streams.Verbose.DataAdded -= DataAdded;
                _streams.Debug.DataAdded -= DataAdded;
                _streams.Information.DataAdded -= DataAdded;
                _streams.Progress.DataAdded -= DataAdded;
                _streams = null;
                _logger = null;
            }

            private void DataAdded(object sender, DataAddedEventArgs e)
            {
                var item = ((IList)sender)[e.Index];
                switch(item)
                {
                    case ErrorRecord er:
                        _logger.LogError(er.ErrorDetails.Message);
                        break;
                    case DebugRecord dr:
                        _logger.LogDebug(dr.Message);
                        break;
                    case VerboseRecord vr:
                        _logger.LogTrace(vr.Message);
                        break;
                    case WarningRecord wr:
                        _logger.LogWarning(wr.Message);
                        break;
                    case InformationalRecord ir:
                        _logger.LogInformation(ir.Message);
                        break;
                    case ProgressRecord pr:
                        _logger.LogInformation($"{pr.PercentComplete}% {pr.Activity}: {pr.StatusDescription}");
                        break;
                }
            }
        }
    }
}
