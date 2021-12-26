using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Media;
using System.IO;
using System.Drawing;
using Microsoft.WindowsAPICodePack.Shell;
//using System.Data.Entity;
using System.Data;
using System.Data.Linq;
using System.Data.SqlClient;

namespace lab3a
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private bool mediaPlayerIsPlaying = false;
		private bool mediaPlayerCanBeStopped = false;
		private bool userIsDraggingSlider = false;
		private readonly MediaPlayer mediaPlayer = new MediaPlayer();
		private String[] paths;
		private DBTestEntities context = new DBTestEntities();

        public MainWindow()
		{
			InitializeComponent();
            CheckAndDelete();
            Load_list();

			sliderVolume.Value = 0.5;
			mediaPlayer.MediaEnded += Media_Ended;

			// Timer
			System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer
			{
				Interval = TimeSpan.FromSeconds(1)
			};
			timer.Tick += timer_Tick;
			timer.Start();
		}

		private void CheckAndDelete()
        {
			foreach (Song song in context.Songs)
            {
				int id;
                if (System.IO.Path.GetExtension(song.Path) != ".mp3" ||
                    !File.Exists(song.Path))
                {
					id = song.Id;
					context.Songs.Remove(song);

					foreach (Band_Song b_s in context.Band_Song.Where(bs => bs.Song_id == id))
					{
						context.Band_Song.Remove(b_s);
					}
				}
            }

			context.SaveChanges();
		}

		private void btnRefresh_Click(object sender, CanExecuteRoutedEventArgs e)
		{
			CheckAndDelete();
			Refresh_list();
		}

		private void Load_list()
		{
			songList.Focus();
			songList.DataContext = TableLoad();
        }

		private List<SongView> TableLoad()
		{
			List<SongView> listS = new List<SongView>();

			foreach (var item in context.Songs)
			{
				string title;
				string band;

				if (item.Band_Song.Count > 0)
				{
					List<string> str = new List<string>();
					foreach (Band_Song b_s in item.Band_Song)
					{
						str.Add(b_s.Band.Name);
					}
					band = String.Join(" & ", str);
				}
				else
				{
					band = "unknown";
				}

				if (item.Name == null)
				{
					title = "unknown";
				}
				else
				{
					title = item.Name;
				}

				listS.Add(new SongView
				{
					Id = item.Id,
					Name = title,
					Path = item.Path,
					Band = band
				});
			}
			return listS;
		}

		private void Refresh_list()
		{
			songList.Focus();
			songList.DataContext = TableLoad();
		}

		// Open button
		private void btnOpen_Executed(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
			{
				Multiselect = true,
				Filter = "MP3 files (*.mp3)|*.mp3"
			};
			if (openFileDialog.ShowDialog() == true)
			{
				paths = openFileDialog.FileNames;
				AddAndRefresh();
			}
		}

		private void btnOpen_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void AddAndRefresh()
		{
			foreach (string fileName in paths)
			{
				AddMusic(fileName);
			}

            Refresh_list();
        }

		private void AddMusic(string fileName)
		{
			if (context.Songs.Any(s => s.Path.Equals(fileName))) return;

			TagLib.File tagFile = TagLib.File.Create(fileName);
			List<string> artists = new List<string>();
            artists.AddRange(tagFile.Tag.Performers);
            artists.Add(tagFile.Tag.FirstPerformer);
            string title = tagFile.Tag.Title;

            List<int> idB = new List<int>();
			int idS;

            int idband = -1;
            foreach (string artist in artists)
            {
				if (String.IsNullOrEmpty(artist)) continue;
                if (context.Bands.Any(s => s.Name.Equals(artist)))
                {
                    if (idband == -1)
                    {
                        idband = context.Bands.Where(b => b.Name.Equals(artist)).First().Id;
                    }
                    else
                    {
                        idband++;
                    }

                    if (!idB.Any(i => i.Equals(idband)))
                        idB.Add(idband);

                    continue;
                }

                if (idband == -1)
                {
                    idband = context.Bands.Add(new Band
                    {
                        Name = artist
                    }
                    ).Id;
                }
                else
                {
					idband++;

					context.Bands.Add(new Band
                    {
                        Id = idband,
                        Name = artist
                    }
                    );

                    if (!idB.Any(i => i.Equals(idband)))
                        idB.Add(idband);
                }
            }

            if (String.IsNullOrEmpty(title))
                idS = context.Songs.Add(new Song
					{
						Name = null,
						Path = fileName
					}
				).Id;
            else
                idS = context.Songs.Add(new Song
					{
						Name = title,
						Path = fileName
					}
                ).Id;

            if (idB.Count() > 0)
            {
                int id = -1;
                foreach (int artist in idB)
                {
                    if (id == -1)
                    {
                        id = context.Band_Song.Add(new Band_Song
                        {
                            Song_id = idS,
                            Band_id = artist
                        }
                        ).Id;
                        continue;
                    }

                    context.Band_Song.Add(new Band_Song
                    {
                        Id = ++id,
                        Song_id = idS,
                        Band_id = artist
                    }
                    );

                }
            }

            context.SaveChanges();

		}

		private void openFile(String fileName)
		{
			mediaPlayer.Open(new Uri(fileName));

			// Get thumbnail
			BitmapImage bitmap = new BitmapImage();

			ShellFile shellFile = ShellFile.FromFilePath(fileName);
			Bitmap shellThumb = shellFile.Thumbnail.ExtraLargeBitmap;
			MemoryStream memory = new MemoryStream();
			shellThumb.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
			memory.Position = 0;
			bitmap.BeginInit();
			bitmap.StreamSource = memory;
			bitmap.CacheOption = BitmapCacheOption.OnLoad;
			bitmap.EndInit();

			image.Source = bitmap;

			// Play
			mediaPlayer.Play();
			mediaPlayerIsPlaying = mediaPlayerCanBeStopped = true;
		}

		// Timer
		void timer_Tick(object sender, EventArgs e)
		{
			if ((mediaPlayer.Source != null) && mediaPlayer.NaturalDuration.HasTimeSpan && !userIsDraggingSlider)
			{
				lblStatus.Content = String.Format("{0} / {1}", mediaPlayer.Position.ToString(@"mm\:ss"), mediaPlayer.NaturalDuration.TimeSpan.ToString(@"mm\:ss"));
				sliderSongProgress.Value = mediaPlayer.Position.TotalSeconds;
				sliderSongProgress.Minimum = 0;
				sliderSongProgress.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
			}
			else
				lblStatus.Content = "00:00 / 00:00";
		}

		// Play button
		private void btnPlay_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = (mediaPlayer != null) && (mediaPlayer.Source != null) && !mediaPlayerIsPlaying;
		}

		private void btnPlay_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			mediaPlayer.Play();
			mediaPlayerIsPlaying = mediaPlayerCanBeStopped = true;
		}

		// Pause button
		private void btnPause_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = mediaPlayerIsPlaying;
		}

		private void btnPause_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			mediaPlayer.Pause();
			mediaPlayerIsPlaying = false;
		}

		// Stop button
		private void btnStop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = mediaPlayerCanBeStopped;
		}

		private void btnStop_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			mediaPlayer.Stop();
			mediaPlayerCanBeStopped = mediaPlayerIsPlaying = false;
		}

		// slider Progress
		private void sliderSongProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			mediaPlayer.Position = TimeSpan.FromSeconds(sliderSongProgress.Value);
		}

		private void sliderSongProgress_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
		{
			mediaPlayer.Pause();
			userIsDraggingSlider = true;
		}

		private void sliderSongProgress_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			userIsDraggingSlider = false;
			mediaPlayer.Position = TimeSpan.FromSeconds(sliderSongProgress.Value);
			mediaPlayer.Play();
		}

		// slider Volume
		private void sliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			mediaPlayer.Volume = sliderVolume.Value;
		}

		// mousewheel volume
		private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			sliderVolume.Value += (e.Delta > 0) ? 0.1 : -0.1;
		}

		// songList
		private void songList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
            if ((songList.SelectedIndex < songList.Items.Count) && (songList.SelectedIndex >= 0))
            {
                openFile(songPath.Text);
                return;
            }
            else
            {
                songList.SelectedIndex = 0;
            }

        }

		// Media ended
		private void Media_Ended(object sender, EventArgs e)
		{
			//if (songList.SelectedIndex < songList.Items.Count)
			songList.SelectedIndex++;
		}

		// prev btn
		private void btnPrevious_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = songList.SelectedIndex > 0;
		}

		private void btnPrevious_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			songList.SelectedIndex--;
		}

		// next btn
		private void btnNext_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = songList.SelectedIndex < songList.Items.Count - 1;
		}

		private void btnNext_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			songList.SelectedIndex++;
		}

		// play pause toggle
		private void playPause_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = (mediaPlayer != null) && (mediaPlayer.Source != null);
		}

		private void playPause_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (mediaPlayerIsPlaying)
			{
				mediaPlayer.Pause();
				mediaPlayerIsPlaying = false;
			}
			else
			{
				mediaPlayer.Play();
				mediaPlayerIsPlaying = mediaPlayerCanBeStopped = true;
			}
		}

		private void btnRefresh_Click(object sender, RoutedEventArgs e)
		{
			Refresh_list();
		}
	}

	public class SongView
    {
		public int Id { get; set; }
		public string Name { get; set; }
		public string Path { get; set; }
		public string Band { get; set; }
	}
}