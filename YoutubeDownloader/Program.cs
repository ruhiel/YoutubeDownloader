using System;
using VideoLibrary;
using NReco.VideoConverter;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;
using NAudio.Wave;

namespace YoutubeDownloader
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: false);

            app.Name = nameof(Program);
            app.Description = "cURL みたいな単機能 CLI";
            app.HelpOption("-h|--help");

            var pathOption = app.Option(
                template: "--path",
                description: "保存先ディレクトリ",
                optionType: CommandOptionType.SingleValue);

            var urlArgument = app.Argument(
                name: "url",
                description: "URL",
                multipleValues: false);

            var wavOption = app.Option(
                template: "--wav",
                description: "wav形式に変換する",
                optionType: CommandOptionType.NoValue);

            app.OnExecute(() =>
            {
                if (urlArgument.Value == null || !pathOption.HasValue())
                {
                    app.ShowHelp();
                    return 1;
                }
                
                Console.WriteLine("ダウンロード中");

                var path = pathOption.Value();

                // ダウンロードするYouTube動画のURL
                string videoUrl = urlArgument.Value;

                // YouTubeの動画オブジェクトを作成する
                var youtube = YouTube.Default;

                // 動画情報を取得する
                var video = youtube.GetVideo(videoUrl);

                // 動画をダウンロードする
                var mp4FilePath = Path.Combine(path, video.FullName);

                File.WriteAllBytes(mp4FilePath, video.GetBytes());

                Console.WriteLine("mp3変換中");

                // FFmpegコンバータをセットアップする
                var ffMpeg = new FFMpegConverter();

                // mp4をmp3に変換する
                var mp3FilePath = Path.ChangeExtension(mp4FilePath, ".mp3");
                ffMpeg.ConvertMedia(mp4FilePath, mp3FilePath, "mp3");

                // 元のmp4ファイルを削除する
                File.Delete(mp4FilePath);

                if(wavOption.HasValue())
                {
                    Console.WriteLine("wav変換中");
                    WaveFormat format = new WaveFormat();
                    Mp3FileReader reader = new Mp3FileReader(mp3FilePath);

                    var wavFilePath = Path.ChangeExtension(mp4FilePath, ".wav");

                    using (WaveFormatConversionStream stream = new WaveFormatConversionStream(format, reader))
                    {
                        WaveFileWriter.CreateWaveFile(wavFilePath, stream);
                    }
                }

                // 成功メッセージを出力する
                Console.WriteLine("動画のダウンロードとファイル変換が成功しました。");

                return 0;
            });

            app.Execute(args);
        }
    }
}
