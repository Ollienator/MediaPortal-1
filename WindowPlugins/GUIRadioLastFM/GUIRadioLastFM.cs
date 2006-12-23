#region Copyright (C) 2005-2007 Team MediaPortal

/* 
 *	Copyright (C) 2005-2007 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using MediaPortal.Configuration;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.Music.Database;
using MediaPortal.Player;
using MediaPortal.TagReader;
using MediaPortal.Util;

namespace MediaPortal.GUI.RADIOLASTFM
{
  [PluginIcons("WindowPlugins.GUIRadioLastFM.BallonRadio.gif", "WindowPlugins.GUIRadioLastFM.BallonRadioDisabled.gif")]  
  public class GUIRadioLastFM : GUIWindow, ISetupForm, IShowPlugin
  {
    private enum SkinControlIDs
    {
      BTN_START_STREAM = 10,
      BTN_CHOOSE_TAG = 20,
      BTN_CHOOSE_FRIEND = 30,
      LIST_TRACK_TAGS = 55,
      IMG_ARTIST_ART = 112,
    }

    [SkinControlAttribute((int)SkinControlIDs.BTN_START_STREAM)]    protected GUIButtonControl btnStartStream = null;
    [SkinControlAttribute((int)SkinControlIDs.BTN_CHOOSE_TAG)]      protected GUIButtonControl btnChooseTag = null;
    [SkinControlAttribute((int)SkinControlIDs.BTN_CHOOSE_FRIEND)]   protected GUIButtonControl btnChooseFriend = null;
    //[SkinControlAttribute((int)SkinControlIDs.LIST_TRACK_TAGS)]     protected GUIListControl facadeTrackTags = null;
    [SkinControlAttribute((int)SkinControlIDs.IMG_ARTIST_ART)]      protected GUIImage imgArtistArt = null;

    private AudioscrobblerUtils InfoScrobbler = null;
    private StreamControl LastFMStation = null;
    private NotifyIcon _trayBallonSongChange = null;
    private bool _configShowTrayIcon = true;
    private bool _configShowBallonTips = true;
    private bool _configSubmitToProfile = true;
    private int _configListEntryCount = 12;
    private List<string> _usersOwnTags = null;
    private List<string> _usersFriends = null;
    private ScrobblerUtilsRequest _lastTrackTagRequest;
    private ScrobblerUtilsRequest _lastArtistCoverRequest;
    private ScrobblerUtilsRequest _lastSimilarArtistRequest;
    private ScrobblerUtilsRequest _lastUsersTagsRequest;
    private ScrobblerUtilsRequest _lastUsersFriendsRequest;

    // constructor
    public GUIRadioLastFM()
    {
      GetID = (int)GUIWindow.Window.WINDOW_RADIO_LASTFM;
    }


    public override bool Init()
    {
      bool bResult = Load(GUIGraphicsContext.Skin + @"\MyRadioLastFM.xml");

      using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
      {
        _configShowTrayIcon = xmlreader.GetValueAsBool("audioscrobbler", "showtrayicon", true);
        _configShowBallonTips = xmlreader.GetValueAsBool("audioscrobbler", "showballontips", true);
        _configSubmitToProfile = xmlreader.GetValueAsBool("audioscrobbler", "submitradiotracks", true);
        _configListEntryCount = xmlreader.GetValueAsInt("audioscrobbler", "listentrycount", 12);
      }
      
      LastFMStation = new StreamControl();
      InfoScrobbler = AudioscrobblerUtils.Instance;
      _usersOwnTags = new List<string>();
      _usersFriends = new List<string>();

      if (_configShowTrayIcon)
        InitTrayIcon();

      //g_Player.PlayBackStarted += new g_Player.StartedHandler(g_Player_PlayBackStarted);
      g_Player.PlayBackStopped += new g_Player.StoppedHandler(PlayBackStoppedHandler);
      g_Player.PlayBackEnded += new g_Player.EndedHandler(PlayBackEndedHandler);

      LastFMStation.RadioSettingsSuccess +=new StreamControl.RadioSettingsLoaded(OnRadioSettingsSuccess);
      LastFMStation.RadioSettingsError +=new StreamControl.RadioSettingsFailed(OnRadioSettingsError);

      LastFMStation.StreamSongChanged += new StreamControl.SongChangedHandler(OnLastFMStation_StreamSongChanged);
      
      return bResult;
    }

    private void OnRadioSettingsSuccess()
    {
      UpdateUsersTags(LastFMStation.AccountUser);
      UpdateUsersFriends(LastFMStation.AccountUser);
      GUIWaitCursor.Hide();

      btnStartStream.Selected = true;
    }

    private void OnRadioSettingsError()
    {
      GUIWaitCursor.Hide();

      GUIDialogOK msgdlg = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
      if (msgdlg == null)
        return;
      msgdlg.SetHeading(GUILocalizeStrings.Get(34054)); // Radio handshake failed!
      msgdlg.SetLine(1, GUILocalizeStrings.Get(34055)); // Streams might be temporarily unavailable
      msgdlg.DoModal(GetID);

      btnStartStream.Selected = false;
    }

    #region Serialisation
    private void LoadSettings()
    {
      GUIWaitCursor.Show();
      BackgroundWorker worker = new BackgroundWorker();
      worker.DoWork += new DoWorkEventHandler(Worker_LoadSettings);
      worker.RunWorkerAsync(); 
    }

    private void Worker_LoadSettings(object sender, DoWorkEventArgs e)
    {
      if (!LastFMStation.IsInit)
      {
        LastFMStation.LoadConfig();
        LastFMStation.SubmitRadioSongs = _configSubmitToProfile;
      }   
      else
        GUIWaitCursor.Hide();
    }

    //void SaveSettings()
    //{
    //  using (MediaPortal.Profile.Settings xmlwriter = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
    //  {
    //  }
    //}
    #endregion


    #region BaseWindow Members
    public override void OnAction(Action action)
    {
      if (action.wID == Action.ActionType.ACTION_NEXT_ITEM && (int)LastFMStation.CurrentStreamState > 2)
      {
        LastFMStation.SendControlCommand(StreamControls.skiptrack);
      }

      base.OnAction(action);
    }

    protected override void OnPageLoad()
    {
      base.OnPageLoad();
      
      if (_trayBallonSongChange != null)
        _trayBallonSongChange.Visible = true;

      if (_usersOwnTags.Count < 1)
      {
        btnChooseTag.Disabled = true;
        btnChooseTag.Label = GUILocalizeStrings.Get(34030);
      }

      if (_usersFriends.Count < 1)
      {
        btnChooseFriend.Disabled = true;
        btnChooseFriend.Label = GUILocalizeStrings.Get(34031);
      }
      GUIPropertyManager.SetProperty("#trackduration", " ");

      String ThumbFileName = String.Empty;

      if (LastFMStation.CurrentTrackTag != null && LastFMStation.CurrentTrackTag.Artist != String.Empty)
        ThumbFileName = Util.Utils.GetCoverArtName(Thumbs.MusicArtists, LastFMStation.CurrentTrackTag.Artist);

      SetArtistThumb(ThumbFileName);

      LoadSettings();
    }

    protected override void OnPageDestroy(int newWindowId)
    {
      if (_trayBallonSongChange != null)
        _trayBallonSongChange.Visible = false;

      base.OnPageDestroy(newWindowId);
    }

    public override void DeInit()
    {
      if (_trayBallonSongChange != null)
      {
        _trayBallonSongChange.Visible = false;
        _trayBallonSongChange = null;
      }

      if (_lastTrackTagRequest != null)
        InfoScrobbler.RemoveRequest(_lastTrackTagRequest);
      if (_lastArtistCoverRequest != null)
        InfoScrobbler.RemoveRequest(_lastArtistCoverRequest);
      if (_lastSimilarArtistRequest != null)
        InfoScrobbler.RemoveRequest(_lastSimilarArtistRequest);
      if (_lastUsersTagsRequest != null)
        InfoScrobbler.RemoveRequest(_lastUsersTagsRequest);
      if (_lastUsersFriendsRequest != null)
        InfoScrobbler.RemoveRequest(_lastUsersFriendsRequest);

      base.DeInit();
    }

    protected override void OnClicked(int controlId, GUIControl control, MediaPortal.GUI.Library.Action.ActionType actionType)
    {
      if (control == btnStartStream)
      {
        bool isSubscriber = LastFMStation.IsSubscriber;
        String desiredTag = String.Empty;
        String desiredFriend = String.Empty;
        StreamType TuneIntoSelected = LastFMStation.CurrentTuneType;

        GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
        if (dlg == null)
          return;
        dlg.Reset();
        dlg.SetHeading(34001);                   // Start Stream
// 1
        dlg.Add(GUILocalizeStrings.Get(34040));  // Recommendation radio
// 2
        dlg.Add("MediaPortal User's group radio");        

// 3
        if (btnChooseTag.Label != String.Empty)
          desiredTag = GUILocalizeStrings.Get(34041) + btnChooseTag.Label;  // Tune into chosen Tag: 
        else
          desiredTag = GUILocalizeStrings.Get(34042);                       // No tag has been chosen yet
        dlg.Add(desiredTag);        

// 4
        if (btnChooseFriend.Label != String.Empty)
          desiredFriend = GUILocalizeStrings.Get(34043) + btnChooseFriend.Label; // Personal radio of: 
        else
          desiredFriend = GUILocalizeStrings.Get(34045); // No Friend has been chosen yet
        dlg.Add(desiredFriend);

// 5
        if (btnChooseFriend.Label != String.Empty)
          desiredFriend = GUILocalizeStrings.Get(34044) + btnChooseFriend.Label; // Loved tracks of: 
        else
          desiredFriend = GUILocalizeStrings.Get(34045); // No Friend has been chosen yet
        dlg.Add(desiredFriend);

// 6
        dlg.Add(GUILocalizeStrings.Get(34048));      // My neighbour radio  

        if (isSubscriber)
        {
// 7
          dlg.Add(GUILocalizeStrings.Get(34046)); // My personal radio
// 8
          dlg.Add(GUILocalizeStrings.Get(34047)); // My loved tracks
        }
        
        dlg.DoModal(GetID);
        if (dlg.SelectedId == -1)
          return;

        // dlg starts with 1...
        switch (dlg.SelectedId)
        {
          case 1:
            TuneIntoSelected = StreamType.Recommended;
            LastFMStation.StreamsUser = LastFMStation.AccountUser;
            break;
          case 2:
            TuneIntoSelected = StreamType.Group;
            LastFMStation.StreamsUser = "MediaPortal Users";
            break;          
          case 3:
            // bail out if no tags available
            if (btnChooseTag.Label == GUILocalizeStrings.Get(34030))
              return;
            TuneIntoSelected = StreamType.Tags;            
            break;
          case 4:
            // bail out if no friends have been made
            if (btnChooseFriend.Label == GUILocalizeStrings.Get(34031))
              return;
            TuneIntoSelected = StreamType.Personal;
            LastFMStation.StreamsUser = btnChooseFriend.Label;
            break;
          case 5:
            // bail out if no friends have been made
            if (btnChooseFriend.Label == GUILocalizeStrings.Get(34031))
              return;
            TuneIntoSelected = StreamType.Loved;
            LastFMStation.StreamsUser = btnChooseFriend.Label;
            break;
          case 6:
            TuneIntoSelected = StreamType.Neighbours;
            LastFMStation.StreamsUser = LastFMStation.AccountUser;
            break;  
          case 7:
            TuneIntoSelected = StreamType.Personal;
            LastFMStation.StreamsUser = LastFMStation.AccountUser;
            break;
          case 8:
            TuneIntoSelected = StreamType.Loved;
            LastFMStation.StreamsUser = LastFMStation.AccountUser;
            break;
          default:
            return;
        }
        if (LastFMStation.CurrentStreamState == StreamPlaybackState.nocontent)
          LastFMStation.CurrentStreamState = StreamPlaybackState.initialized;

        g_Player.Stop();
        // LastFMStation.CurrentTuneType = TuneIntoSelected;
        switch (TuneIntoSelected)
        {
          case StreamType.Recommended:
            LastFMStation.TuneIntoRecommendedRadio(LastFMStation.StreamsUser);
            break;

          case StreamType.Group:
            LastFMStation.TuneIntoGroupRadio(LastFMStation.StreamsUser);
            break;

          case StreamType.Personal:
            LastFMStation.TuneIntoPersonalRadio(LastFMStation.StreamsUser);
            break;

          case StreamType.Loved:
            LastFMStation.TuneIntoLovedTracks(LastFMStation.StreamsUser);
            break;

          case StreamType.Tags:
            List<String> MyTags = new List<string>();
            MyTags.Add(btnChooseTag.Label);
            //MyTags.Add("melodic death metal");
            LastFMStation.TuneIntoTags(MyTags);
            break;
            
          case StreamType.Neighbours:
            LastFMStation.TuneIntoNeighbourRadio(LastFMStation.StreamsUser);
            break;
        }

        if (LastFMStation.CurrentStreamState == StreamPlaybackState.initialized)
        {
          if (!LastFMStation.PlayStream())
          {
            GUIDialogOK msgdlg = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
            if (msgdlg == null)
              return;
            msgdlg.SetHeading(34050); // No stream active
            msgdlg.SetLine(1, GUILocalizeStrings.Get(34053)); // Playback of selected stream failed
            msgdlg.DoModal(GetID);
          }
        }
        else
          Log.Info("GUIRadio: Didn't start LastFM radio because stream state is {0}", LastFMStation.CurrentStreamState.ToString());
      }

      if (control == btnChooseTag)
      {
        GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
        if (dlg == null)
          return;
        dlg.Reset();
        dlg.SetHeading(33013); // tracks suiting configured tag
        foreach (string ownTag in _usersOwnTags)
          dlg.Add(ownTag);

        dlg.DoModal(GetID);
        if (dlg.SelectedId == -1)
          return;
        btnChooseTag.Label = _usersOwnTags[dlg.SelectedId -1];
        GUIPropertyManager.SetProperty("#selecteditem", btnChooseTag.Label);
      }

      if (control == btnChooseFriend)
      {
        GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
        if (dlg == null)
          return;
        dlg.Reset();
        dlg.SetHeading(33016); // tracks your friends like
        foreach (string Friend in _usersFriends)
          dlg.Add(Friend);

        dlg.DoModal(GetID);
        if (dlg.SelectedId == -1)
          return;
        btnChooseFriend.Label = _usersFriends[dlg.SelectedId - 1];
        GUIPropertyManager.SetProperty("#selecteditem", btnChooseFriend.Label);
      }

      base.OnClicked(controlId, control, actionType);
    }

    public override bool OnMessage(GUIMessage message)
    {
      switch (message.Message)
      {
        case GUIMessage.MessageType.GUI_MSG_SHOW_BALLONTIP_SONGCHANGE:
          if (_configShowBallonTips)
            ShowSongTrayBallon(message.Label, message.Label2, message.Param1, true);
          break;
      }
      return base.OnMessage(message);
    }

    protected override void OnShowContextMenu()
    {
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);

      if (dlg == null)
        return;

      dlg.Reset();
      dlg.SetHeading(924);                  // Menu

      dlg.AddLocalizedString(34010);        // Love
      dlg.AddLocalizedString(34011);        // Ban
      dlg.AddLocalizedString(34012);        // Skip

      if (LastFMStation.CurrentTrackTag != null)
        dlg.AddLocalizedString(33040);  // copy IRC spam

      dlg.DoModal(GetID);

      if (dlg.SelectedId == -1)
        return;

      switch (dlg.SelectedId)
      {
        case 34010:     // Love
          LastFMStation.SendControlCommand(StreamControls.lovetrack);
          break;
        case 34011:     // Ban
          LastFMStation.SendControlCommand(StreamControls.bantrack);
          break;
        case 34012:     // Skip
          LastFMStation.SendControlCommand(StreamControls.skiptrack);
          break;

        case 33040:    // IRC spam          
          try
          {
            if (LastFMStation.CurrentTrackTag != null)
            {
              string tmpTrack = LastFMStation.CurrentTrackTag.Track > 0 ? (Convert.ToString(LastFMStation.CurrentTrackTag.Track) + ". ") : String.Empty;
              Clipboard.SetDataObject(@"/me is listening on last.fm: " + LastFMStation.CurrentTrackTag.Artist + " [" + LastFMStation.CurrentTrackTag.Album + "] - " + tmpTrack + LastFMStation.CurrentTrackTag.Title, true);
            }
          }
          catch (Exception ex)
          {
            Log.Error("GUIRadioLastFM: could not copy song spam to clipboard - {0}", ex.Message);
          }
          break;

      }
    }
    #endregion


    #region Internet Lookups
    private void UpdateUsersFriends(string _serviceUser)
    {
      UsersFriendsRequest request = new UsersFriendsRequest(
              _serviceUser,
              new UsersFriendsRequest.UsersFriendsRequestHandler(OnUpdateUsersFriendsCompleted));
      _lastUsersFriendsRequest = request;
      InfoScrobbler.AddRequest(request);
    }

    private void UpdateUsersTags(string _serviceUser)
    {
      UsersTagsRequest request = new UsersTagsRequest(
              _serviceUser,
              new UsersTagsRequest.UsersTagsRequestHandler(OnUpdateUsersTagsCompleted));
      _lastUsersTagsRequest = request;
      InfoScrobbler.AddRequest(request);
    }

    private void UpdateArtistInfo(string _trackArtist)
    {
      if (_trackArtist == null)
        return;
      if (_trackArtist != String.Empty)
      {
        ArtistInfoRequest request = new ArtistInfoRequest(
                      _trackArtist,
                      new ArtistInfoRequest.ArtistInfoRequestHandler(OnUpdateArtistCoverCompleted));
        _lastArtistCoverRequest = request;
        InfoScrobbler.AddRequest(request);

        SimilarArtistRequest request2 = new SimilarArtistRequest(
                      _trackArtist,
                      false,
                      new SimilarArtistRequest.SimilarArtistRequestHandler(OnUpdateSimilarArtistsCompleted));
        _lastSimilarArtistRequest = request2;
        InfoScrobbler.AddRequest(request2);
      }
    }

    private void UpdateTrackTagsInfo(string _trackArtist, string _trackTitle)
    {
      TagsForTrackRequest request = new TagsForTrackRequest(
                      _trackArtist,
                      _trackTitle,
                      new TagsForTrackRequest.TagsForTrackRequestHandler(OnUpdateTrackTagsInfoCompleted));
      _lastTrackTagRequest = request;
      InfoScrobbler.AddRequest(request);
    }

    public void OnUpdateArtistCoverCompleted(ArtistInfoRequest request, Song song)
    {
      if (request.Equals(_lastArtistCoverRequest))
      {
        String ThumbFileName = Util.Utils.GetCoverArtName(Thumbs.MusicArtists, LastFMStation.CurrentTrackTag.Artist);
        if (ThumbFileName.Length > 0)
        {
          SetArtistThumb(ThumbFileName);
        }
      }
      else
      {
        Log.Warn("NowPlaying.OnUpdateArtistInfoCompleted: unexpected response for request: {0}", request.Type);
      }
    }

    public void OnUpdateSimilarArtistsCompleted(SimilarArtistRequest request2, List<Song> SimilarArtists)
    {
      if (request2.Equals(_lastSimilarArtistRequest))
      {
        String propertyTags = String.Empty;

        for (int i = 0; i < SimilarArtists.Count; i++)
        {
          // some artist names might be very long - reduce the number of tags then
          if (propertyTags.Length > 50)
            break;

          propertyTags += SimilarArtists[i].Artist + "   ";

          // display 5 items only
          if (i >= 4)
            break;
        }
        GUIPropertyManager.SetProperty("#Play.Current.Lastfm.SimilarArtists", propertyTags);
      }
      else
      {
        Log.Warn("NowPlaying.OnUpdateSimilarArtistsCompleted: unexpected response for request: {0}", request2.Type);
      }
    }

    public void OnUpdateTrackTagsInfoCompleted(TagsForTrackRequest request, List<Song> TagTracks)
    {
      if (request.Equals(_lastTrackTagRequest))
      {
        String propertyTags = String.Empty;

        for (int i = 0; i < TagTracks.Count; i++)
        {
          // some tags might be very long - reduce the number of tags then
          if (propertyTags.Length > 50)
            break;

          propertyTags += TagTracks[i].Genre + "   ";

          // display 5 items only
          if (i >= 4)
            break;
        }
        GUIPropertyManager.SetProperty("#Play.Current.Lastfm.TrackTags", propertyTags);
      }
      else
      {
        Log.Warn("NowPlaying.OnUpdateTrackTagsInfoCompleted: unexpected response for request: {0}", request.Type);
      }
    }

    public void OnUpdateUsersTagsCompleted(UsersTagsRequest request, List<Song> FeedItems)
    {
      if (request.Equals(_lastUsersTagsRequest))
      {
        if (_usersOwnTags != null)
          _usersOwnTags.Clear();
        for (int i = 0; i < FeedItems.Count; i++)
        {
          _usersOwnTags.Add(FeedItems[i].Artist);
          if (i == _configListEntryCount -1)
            break;
        }
        if (_usersOwnTags.Count > 0)
        {
          btnChooseTag.Disabled = false;
          btnChooseTag.Label = _usersOwnTags[0];
        }
      }
      else
        Log.Warn("NowPlaying.OnUpdateUsersTagsCompleted: unexpected response for request: {0}", request.Type);
    }

    public void OnUpdateUsersFriendsCompleted(UsersFriendsRequest request, List<Song> FeedItems)
    {
      if (request.Equals(_lastUsersFriendsRequest))
      {
        if (_usersFriends != null)
          _usersFriends.Clear();
        for (int i = 0; i < FeedItems.Count; i++)
        {
          _usersFriends.Add(FeedItems[i].Artist);
          if (i == _configListEntryCount -1)
            break;
        }
        if (_usersFriends.Count > 0)
        {
          btnChooseFriend.Disabled = false;
          btnChooseFriend.Label = _usersFriends[0];
        }
      }
      else
        Log.Warn("NowPlaying.OnUpdateUsersFriendsCompleted: unexpected response for request: {0}", request.Type);
    }

    private void OnPlaybackStopped()
    {
      LastFMStation.CurrentTrackTag.Clear();
      LastFMStation.CurrentStreamState = StreamPlaybackState.initialized;

      SetArtistThumb(String.Empty);
      GUIPropertyManager.SetProperty("#Play.Current.Lastfm.TrackTags", String.Empty);
      GUIPropertyManager.SetProperty("#Play.Current.Lastfm.SimilarArtists", String.Empty);
      GUIPropertyManager.SetProperty("#trackduration", " ");
      GUIPropertyManager.SetProperty("#currentplaytime", String.Empty);
      GUIPropertyManager.SetProperty("#Play.Current.Lastfm.CurrentStream", String.Empty);

      //reset the TrayIcon
      ShowSongTrayBallon(GUILocalizeStrings.Get(34050), " ", 1, false); // Stream stopped
    }
    #endregion


    #region Handlers
    private void OnLastFMStation_StreamSongChanged(MusicTag newCurrentSong, DateTime startTime)
    {
      SetArtistThumb(String.Empty);
      GUIPropertyManager.SetProperty("#Play.Current.Lastfm.TrackTags", String.Empty);
      GUIPropertyManager.SetProperty("#Play.Current.Lastfm.SimilarArtists", String.Empty);

      if (_lastTrackTagRequest != null)
        InfoScrobbler.RemoveRequest(_lastTrackTagRequest);
      if (_lastArtistCoverRequest != null)
        InfoScrobbler.RemoveRequest(_lastArtistCoverRequest);


      if (LastFMStation.CurrentTrackTag != null)
      {
        if (LastFMStation.CurrentTrackTag.Artist != String.Empty)
        {
          UpdateArtistInfo(newCurrentSong.Artist);

          if (LastFMStation.CurrentTrackTag.Title != String.Empty)
            UpdateTrackTagsInfo(LastFMStation.CurrentTrackTag.Artist, LastFMStation.CurrentTrackTag.Title);
        }

        GUIPropertyManager.SetProperty("#Play.Current.Artist", newCurrentSong.Artist);
        GUIPropertyManager.SetProperty("#Play.Current.Album", newCurrentSong.Album);
        GUIPropertyManager.SetProperty("#Play.Current.Title", newCurrentSong.Title);
        GUIPropertyManager.SetProperty("#Play.Current.Genre", newCurrentSong.Genre);
        GUIPropertyManager.SetProperty("#Play.Current.Thumb", newCurrentSong.Comment);
        GUIPropertyManager.SetProperty("#trackduration", Util.Utils.SecondsToHMSString(newCurrentSong.Duration));
      }
    }

    protected void PlayBackStoppedHandler(g_Player.MediaType type, int stoptime, string filename)
    {
      if (!filename.Contains(@"/last.mp3?") || LastFMStation.CurrentStreamState != StreamPlaybackState.streaming)
        return;

      OnPlaybackStopped();
    }

    protected void PlayBackEndedHandler(g_Player.MediaType type, string filename)
    {
      if (!filename.Contains(@"/last.mp3?") || LastFMStation.CurrentStreamState != StreamPlaybackState.streaming)
        return;

      //GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);

      //if (dlg == null)
      //  return;

      //dlg.Reset();
      //dlg.SetHeading(924);                // Menu
      //dlg.Add("No more content for this selection");

      //dlg.DoModal(GetID);

      //if (dlg.SelectedId == -1)
      //  return;
      OnPlaybackStopped();

      ShowSongTrayBallon(GUILocalizeStrings.Get(34051), GUILocalizeStrings.Get(34052), 15, true); // Stream ended, No more content or bad connection

      Log.Info("GUIRadio: No more content for this selection or interrupted stream..");
      LastFMStation.CurrentStreamState = StreamPlaybackState.nocontent;
      //dlg.AddLocalizedString(930);        //Add to favorites
    }

    #endregion


    #region Utils
    void ShowSongTrayBallon(String notifyTitle, String notifyMessage_, int showSeconds_, bool popup_)
    {
      if (_trayBallonSongChange != null)
      {
        // Length may only be 64 chars
        if (notifyTitle.Length > 63)
          notifyTitle = notifyTitle.Remove(63);
        if (notifyMessage_.Length > 63)
          notifyMessage_ = notifyMessage_.Remove(63);

        // XP hides "inactive" icons therefore change the text
        String IconText = "MP Last.fm radio\n" + notifyMessage_ + " - " + notifyTitle;
        if (IconText.Length > 63)
          IconText = IconText.Remove(60) + "..";
        _trayBallonSongChange.Text = IconText;
        _trayBallonSongChange.Visible = true;

        if (notifyTitle == String.Empty)
          notifyTitle = "MediaPortal";
        _trayBallonSongChange.BalloonTipTitle = notifyTitle;
        if (notifyMessage_ == String.Empty)
          notifyMessage_ = IconText;
        _trayBallonSongChange.BalloonTipText = notifyMessage_;
        if (popup_)
          _trayBallonSongChange.ShowBalloonTip(showSeconds_);
      }
    }

    void InitTrayIcon()
    {
      if (_trayBallonSongChange == null)
      {
        ContextMenu contextMenuLastFM = new ContextMenu();
        MenuItem menuItem1 = new MenuItem();
        MenuItem menuItem2 = new MenuItem();
        MenuItem menuItem3 = new MenuItem();

        // Initialize contextMenuLastFM
        contextMenuLastFM.MenuItems.AddRange(new MenuItem[] { menuItem1, menuItem2, menuItem3 });

        // Initialize menuItem1
        menuItem1.Index = 0;
        menuItem1.Text = GUILocalizeStrings.Get(34010); // Love
        menuItem1.Click += new System.EventHandler(Tray_menuItem1_Click);
        // Initialize menuItem2
        menuItem2.Index = 1;
        menuItem2.Text = GUILocalizeStrings.Get(34011); // Ban
        menuItem2.Click += new System.EventHandler(Tray_menuItem2_Click);
        // Initialize menuItem3
        menuItem3.Index = 2;
        menuItem3.Text = GUILocalizeStrings.Get(34012); // Skip
        //menuItem3.Break = true;
        menuItem3.DefaultItem = true;
        menuItem3.Click += new System.EventHandler(Tray_menuItem3_Click);

        _trayBallonSongChange = new NotifyIcon();
        _trayBallonSongChange.ContextMenu = contextMenuLastFM;

        if (System.IO.File.Exists(Config.GetFile(Config.Dir.Base, @"BallonRadio.ico")))
          _trayBallonSongChange.Icon = new Icon(Config.GetFile(Config.Dir.Base, @"BallonRadio.ico"));
        else
          _trayBallonSongChange.Icon = SystemIcons.Information;

        _trayBallonSongChange.Text = "MediaPortal Last.fm Radio";
        _trayBallonSongChange.Visible = false;
      }
    }

    // skip
    void Tray_menuItem1_Click(object Sender, EventArgs e)
    {
      if ((int)LastFMStation.CurrentStreamState > 2)
      {
        LastFMStation.SendControlCommand(StreamControls.lovetrack);
      }
    }

    // ban
    void Tray_menuItem2_Click(object Sender, EventArgs e)
    {
      if ((int)LastFMStation.CurrentStreamState > 2)
      {
        LastFMStation.SendControlCommand(StreamControls.bantrack);
      }
    }

    // love
    void Tray_menuItem3_Click(object Sender, EventArgs e)
    {
      if ((int)LastFMStation.CurrentStreamState > 2)
      {
        LastFMStation.SendControlCommand(StreamControls.skiptrack);
      }
    }

    void SetArtistThumb(string artistThumbPath_)
    {
      string thumb = artistThumbPath_;

      if (thumb.Length <= 0)
        thumb = GUIGraphicsContext.Skin + @"\media\missing_coverart.png";
      else
      {
        // let us test if there is a larger cover art image
        string strLarge = MediaPortal.Util.Utils.ConvertToLargeCoverArt(thumb);
        if (System.IO.File.Exists(strLarge))
        {
          thumb = strLarge;
        }
      }

      //String refString = String.Empty;
      //Util.Utils.GetQualifiedFilename(thumb, ref refString);
      GUIPropertyManager.SetProperty("#Play.Current.ArtistThumb", thumb);

      if (imgArtistArt != null)
        imgArtistArt.SetFileName(thumb);
    }
    #endregion


    #region ISetupForm Members
    public int GetWindowId()
    {
      return GetID;
    }

    public string PluginName()
    {
      return "My Last.fm Radio";
    }

    public string Description()
    {
      return "Listen to radio streams on last.fm - you need to configure the audioscrobbler plugin first!";
    }

    public string Author()
    {
      return "rtv";
    }

    public bool CanEnable()
    {
      bool AudioScrobblerOn = true;
      //using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
      //{
      //  AudioScrobblerOn = xmlreader.GetValueAsBool("plugins", "Audioscrobbler", false);
      //}
      return AudioScrobblerOn;
    }

    public bool DefaultEnabled()
    {
      return false;
    }

    public bool HasSetup()
    {
      return true;
    }

    public bool GetHome(out string strButtonText, out string strButtonImage, out string strButtonImageFocus, out string strPictureImage)
    {
      strButtonText = GUILocalizeStrings.Get(34000);
      strButtonImage = String.Empty;
      strButtonImageFocus = String.Empty;
      strPictureImage = "hover_my radio.png";
      return true;
    }

    // show the setup dialog
    public void ShowPlugin()
    {
      PluginSetupForm lastfmsetup = new PluginSetupForm();
      lastfmsetup.ShowDialog();
    }
    #endregion    

    #region IShowPlugin Members
    public bool ShowDefaultHome()
    {
      return false;
    }
    #endregion
  }
}