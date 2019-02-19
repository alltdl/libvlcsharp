﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using NUnit.Framework;

namespace LibVLCSharp.Tests
{
    [TestFixture]
    public class MediaTests : BaseSetup
    {
        [Test]
        public void CreateMedia()
        {
            var media = new Media(_libVLC, Path.GetTempFileName());

            Assert.AreNotEqual(IntPtr.Zero, media.NativeReference);
        }

        [Test]
        public void CreateMediaFail()
        {
            Assert.Throws<ArgumentNullException>(() => new Media(null, Path.GetTempFileName()));
            Assert.Throws<ArgumentNullException>(() => new Media(_libVLC, string.Empty));
        }

        [Test]
        public void ReleaseMedia()
        {
            var media = new Media(_libVLC, Path.GetTempFileName());

            media.Dispose();

            Assert.AreEqual(IntPtr.Zero, media.NativeReference);
        }

        [Test]
        public void CreateMediaFromStream()
        {
            var media = new Media(_libVLC, new FileStream(Path.GetTempFileName(), FileMode.OpenOrCreate));
            Assert.AreNotEqual(IntPtr.Zero, media.NativeReference);
        }

        [Test]
        public void AddOption()
        {
            var media = new Media(_libVLC, new FileStream(Path.GetTempFileName(), FileMode.OpenOrCreate));
            media.AddOption("-sout-all");
        }

        [Test]
        public async Task CreateRealMedia()
        {
            using (var media = new Media(_libVLC, RealMp3Path))
            {
                media.Parse();
                await Task.Delay(100);
                Assert.NotZero(media.Duration);
                using (var mp = new MediaPlayer(media))
                {
                    Assert.True(mp.Play());
                    await Task.Delay(4000); // have to wait a bit for statistics to populate
                    Assert.Greater(media.Statistics.DemuxReadBytes, 0);
                    mp.Stop();
                }
            }
        }

        [Test]
        public void Duplicate()
        {
            var media = new Media(_libVLC, new FileStream(Path.GetTempFileName(), FileMode.OpenOrCreate));
            var duplicate = media.Duplicate();
            Assert.AreNotEqual(duplicate.NativeReference, media.NativeReference);
        }

        [Test]
        public void CreateMediaFromFileStream()
        {
            var media = new Media(_libVLC, new FileStream(RealMp3Path, FileMode.Open, FileAccess.Read, FileShare.Read));
            Assert.AreNotEqual(IntPtr.Zero, media.NativeReference);
        }

        [Test]
        public void SetMetadata()
        {
            var media = new Media(_libVLC, Path.GetTempFileName());
            const string test = "test";
            media.SetMeta(Media.MetadataType.ShowName, test);
            Assert.True(media.SaveMeta());
            Assert.AreEqual(test, media.Meta(Media.MetadataType.ShowName));
        }

        [Test]
        public void GetTracks()
        {
            var media = new Media(_libVLC, RealMp3Path);
            media.Parse();
            Assert.AreEqual(media.Tracks.Single().Data.Audio.Channels, 2);
            Assert.AreEqual(media.Tracks.Single().Data.Audio.Rate, 44100);
        }

        [Test]
        public async Task CreateRealMediaSpecialCharacters()
        {
            using (var media = new Media(_libVLC, RealMp3PathSpecialCharacter, Media.FromType.FromPath))
            {
                Assert.False(media.IsParsed);

                media.Parse();
                await Task.Delay(5000);
                Assert.True(media.IsParsed);
                Assert.AreEqual(Media.MediaParsedStatus.Done, media.ParsedStatus);
                using (var mp = new MediaPlayer(media))
                {
                    Assert.True(mp.Play());
                    await Task.Delay(10000);
                    mp.Stop();
                }
            }
        }

        [Test]
        public async Task CreateMediaFromStreamMultiplePlay()
        {
            using(var mp = new MediaPlayer(_libVLC))
            {
                var media = new Media(_libVLC, File.OpenRead(RealMp3Path));
                mp.Play(media);

                await Task.Delay(1000);

                mp.Time = 60000;

                await Task.Delay(10000); // end reached, rewind stream

                mp.Play(media);
            }
        }

        [Test]
        public async Task CreateMultipleMediaFromStreamPlay()
        {
            var libVLC1 = new LibVLC("--no-audio", "--no-video");
            var libVLC2 = new LibVLC("--no-audio", "--no-video");

            var mp1 = new MediaPlayer(libVLC1);
            var mp2 = new MediaPlayer(libVLC2);

            mp1.Play(new Media(libVLC1, File.OpenRead(RealMp3Path)));
            mp2.Play(new Media(libVLC2, File.OpenRead(RealMp3Path)));

            await Task.Delay(10000);
        }

        private async Task<Stream> GetStreamFromUrl(string url)
        {
            byte[] imageData = null;

            using (var client = new System.Net.Http.HttpClient())
                imageData = await client.GetByteArrayAsync(url);

            return new MemoryStream(imageData);
        }

        private void LibVLC_Log(object sender, LogEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e.Message);
        }
    }
}