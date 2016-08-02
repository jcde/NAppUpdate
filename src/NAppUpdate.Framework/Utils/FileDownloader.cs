using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

using NAppUpdate.Framework.Common;

namespace NAppUpdate.Framework.Utils
{
	public sealed class FileDownloader
	{
		private readonly Uri _uri;
		private const int _bufferSize = 1024;
		public IWebProxy Proxy { get; set; }

		public FileDownloader()
		{
			Proxy = null;
		}


		public FileDownloader(string url)
		{
			_uri = new Uri(url);
		}

		public FileDownloader(Uri uri)
		{
			_uri = uri;
		}

		public byte[] Download()
		{
			using (var client = new WebClient())
				return client.DownloadData(_uri);
		}

		public bool DownloadToFile(string tempLocation)
		{
			return DownloadToFile(tempLocation, null);
		}

        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public bool DownloadToFileAsync(string tempLocation)
        {
            var request = (HttpWebRequest)WebRequest.Create(_uri);
            request.Proxy = Proxy;
            var myRequestState = new RequestState { request = request, tempLocation = tempLocation };
            var result = request.BeginGetResponse(RespCallback, myRequestState);

            // this line implements the timeout, if there is a timeout, the callback fires and the request becomes aborted
            ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, new WaitOrTimerCallback(TimeoutCallback),
                                                   request, 60*1000, true);

            allDone.WaitOne();

            // Release the HttpWebResponse resource.
            myRequestState.response.Close();
            return true;
        }

	    public bool DownloadToFile(string tempLocation, Action<UpdateProgressInfo> onProgress)
        {
            if (!PlatformCheck.CurrentlyRunningInWindows())
            {
                var p = Process.Start("wget", string.Format("{0} -O {1}", _uri, tempLocation));
                p.WaitForExit();
                return p.ExitCode == 0;
            }
            /// return DownloadToFileAsync(tempLocation);
            var request = (HttpWebRequest)WebRequest.Create(_uri);
            request.Proxy = Proxy;

            using (var response = request.GetResponse())
            {
                using (var tempFile = File.Create(tempLocation))
                {
                    using (var responseStream = response.GetResponseStream())
                    {
                        if (responseStream == null)
                            return false;

                        long downloadSize = response.ContentLength;
                        long totalBytes = 0;
                        var buffer = new byte[_bufferSize];
                        const int reportInterval = 1;
                        DateTime stamp = DateTime.Now.Subtract(new TimeSpan(0, 0, reportInterval));
                        int bytesRead;
                        do
                        {
                            bytesRead = responseStream.Read(buffer, 0, buffer.Length);
                            totalBytes += bytesRead;
                            tempFile.Write(buffer, 0, bytesRead);

                            if (onProgress == null || !(DateTime.Now.Subtract(stamp).TotalSeconds >= reportInterval))
                                continue;
                            ReportProgress(onProgress, totalBytes, downloadSize);
                            stamp = DateTime.Now;
                        } while (bytesRead > 0 && !UpdateManager.Instance.ShouldStop);

                        ReportProgress(onProgress, totalBytes, downloadSize);
                        return totalBytes == downloadSize;
                    }
                }
            }
        }

	    public class RequestState
        {
            // This class stores the State of the request.
            public const int BUFFER_SIZE = 1024;
            public StringBuilder requestData;
            public byte[] BufferRead;
            public HttpWebRequest request;
            public HttpWebResponse response;
            public Stream streamResponse;
	        public string tempLocation;

	        public RequestState()
            {
                BufferRead = new byte[BUFFER_SIZE];
                requestData = new StringBuilder("");
                request = null;
                streamResponse = null;
            }
        }
        private  void RespCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                // State of request is asynchronous.
                RequestState myRequestState = (RequestState)asynchronousResult.AsyncState;
                HttpWebRequest myHttpWebRequest = myRequestState.request;
                myRequestState.response = (HttpWebResponse)myHttpWebRequest.EndGetResponse(asynchronousResult);

                // Read the response into a Stream object.
                Stream responseStream = myRequestState.response.GetResponseStream();
                myRequestState.streamResponse = responseStream;

                // Begin the Reading of the contents of the HTML page and print it to the console.
                IAsyncResult asynchronousInputRead = responseStream.BeginRead(myRequestState.BufferRead, 0,RequestState.BUFFER_SIZE, 

                    ReadCallBack, myRequestState);
                return;
            }
            catch (WebException e)
            {
                Console.WriteLine("\nRespCallback Exception raised!");
                Console.WriteLine("\nMessage:{0}", e.Message);
                Console.WriteLine("\nStatus:{0}", e.Status);
            }
            allDone.Set();
        }


	    private static void TimeoutCallback(object state, bool timedOut)
        {
            if (timedOut)
            {
                HttpWebRequest request = state as HttpWebRequest;
                if (request != null)
                {
                    request.Abort();
                }
            }
        }

        private void ReadCallBack(IAsyncResult asyncResult)
        {
            try
            {

                RequestState myRequestState = (RequestState)asyncResult.AsyncState;
                Stream responseStream = myRequestState.streamResponse;
                int read = responseStream.EndRead(asyncResult);
                // Read the HTML page and then print it to the console.
                if (read > 0)
                {
                    myRequestState.requestData.Append(Encoding.ASCII.GetString(myRequestState.BufferRead, 0, read));
                    IAsyncResult asynchronousResult = responseStream.BeginRead(myRequestState.BufferRead, 0, RequestState.BUFFER_SIZE, new AsyncCallback(ReadCallBack), myRequestState);
                    return;
                }
                else
                {
                    Console.WriteLine("\nThe contents of the Html page are : ");
                    if (myRequestState.requestData.Length > 1)
                    {
                        string stringContent;
                        stringContent = myRequestState.requestData.ToString();
                        Console.WriteLine(stringContent);
                        File.WriteAllText(myRequestState.tempLocation, myRequestState.requestData.ToString());
                    }
                    Console.WriteLine("Press any key to continue..........");
                    //Console.ReadLine();

                    responseStream.Close();
                }

            }
            catch (WebException e)
            {
                Console.WriteLine("\nReadCallBack Exception raised!");
                Console.WriteLine("\nMessage:{0}", e.Message);
                Console.WriteLine("\nStatus:{0}", e.Status);
            }
            allDone.Set();
        }

	    private void ReportProgress(Action<UpdateProgressInfo> onProgress, long totalBytes, long downloadSize)
		{
			if (onProgress != null) onProgress(new DownloadProgressInfo
			{
				DownloadedInBytes = totalBytes,
				FileSizeInBytes = downloadSize,
				Percentage = (int)(((float)totalBytes / (float)downloadSize) * 100),
				Message = string.Format("Downloading... ({0} / {1} completed)", ToFileSizeString(totalBytes), ToFileSizeString(downloadSize)),
				StillWorking = totalBytes == downloadSize,
			});
		}

		private string ToFileSizeString(long size)
		{
			if (size < 1000) return String.Format("{0} bytes", size);
			if (size < 1000000) return String.Format("{0:F1} KB", (size / 1000));
			if (size < 1000000000) return String.Format("{0:F1} MB", (size / 1000000));
			if (size < 1000000000000) return String.Format("{0:F1} GB", (size / 1000000000));
			if (size < 1000000000000000) return String.Format("{0:F1} TB", (size / 1000000000000));
			return size.ToString(CultureInfo.InvariantCulture);
		}

		/*
		public void DownloadAsync(Action<byte[]> finishedCallback)
		{
			DownloadAsync(finishedCallback, null);
		}

		public void DownloadAsync(Action<byte[]> finishedCallback, Action<long, long> progressChangedCallback)
		{
			using (var client = new WebClient())
			{
				if (progressChangedCallback != null)
					client.DownloadProgressChanged += (sender, args) => progressChangedCallback(args.BytesReceived, args.TotalBytesToReceive);

				client.DownloadDataCompleted += (sender, args) => finishedCallback(args.Result);
				client.DownloadDataAsync(_uri);
			}
		}*/
	}
}
