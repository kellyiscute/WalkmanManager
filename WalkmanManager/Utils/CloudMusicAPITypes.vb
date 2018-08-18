Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks


Public Class MvResult
	Public Property LoadingPic As String
	Public Property BufferPic As String
	Public Property LoadingPicFs As String
	Public Property BufferPicFs As String
	Public Property Subed As Boolean
	Public Property Data As Data
	Public Property Code As Long
End Class

Public Class Data
	Public Property Id As Long
	Public Property Name As String
	Public Property ArtistId As Long
	Public Property ArtistName As String
	Public Property BriefDesc As String
	Public Property Desc As String
	Public Property Cover As String
	Public Property CoverId As Long
	Public Property PlayCount As Long
	Public Property SubCount As Long
	Public Property ShareCount As Long
	Public Property LikeCount As Long
	Public Property CommentCount As Long
	Public Property Duration As Long
	Public Property NType As Long
	Public Property PublishTime As DateTime
	Public Property Brs As Brs
	Public Property Artists As Artist()
	Public Property IsReward As Boolean
	Public Property CommentThreadId As String
End Class

Public Class MvArtist
	Public Property Id As Long
	Public Property Name As String
End Class

Public Class Brs
	Public Property The480 As String
	Public Property The240 As String
	Public Property The720 As String
End Class

Public Class LyricResult
	Public Property Sgc As Boolean
	Public Property Sfy As Boolean
	Public Property Qfy As Boolean
	Public Property TransUser As LyricUser
	Public Property LyricUser As LyricUser
	Public Property Lrc As Lrc
	Public Property Klyric As Klyric
	Public Property Tlyric As Lrc
	Public Property Code As Long
End Class

Public Class Klyric
	Public Property Version As Long
End Class

Public Class Lrc
	Public Property Version As Long
	Public Property Lyric As String
End Class

Public Class LyricUser
	Public Property Id As Long
	Public Property Status As Long
	Public Property Demand As Long
	Public Property Userid As Long
	Public Property Nickname As String
	Public Property Uptime As Long
End Class

Public Class SongUrls
	Public Property Data As Datum()
	Public Property Code As Long
End Class

Public Class PlayListResult
	Public Property Playlist As Playlist
	Public Property Code As Long
	Public Property Privileges As Privilege()
End Class

Public Class Playlist
	Public Property Subscribers As Object()
	Public Property Subscribed As Boolean
	Public Property Creator As User
	Public Property Tracks As Track()
	Public Property TrackIds As TrackId()
	Public Property CoverImgId As Long
	Public Property CreateTime As Long
	Public Property UpdateTime As Long
	Public Property NewImported As Boolean
	Public Property Privacy As Long
	Public Property SpecialType As Long
	Public Property CommentThreadId As String
	Public Property TrackUpdateTime As Long
	Public Property TrackCount As Long
	Public Property HighQuality As Boolean
	Public Property SubscribedCount As Long
	Public Property CloudTrackCount As Long
	Public Property CoverImgUrl As String
	Public Property PlayCount As Long
	Public Property AdType As Long
	Public Property TrackNumberUpdateTime As Long
	Public Property Description As Object
	Public Property Ordered As Boolean
	Public Property Tags As Object()
	Public Property Status As Long
	Public Property UserId As Long
	Public Property Name As String
	Public Property Id As Long
	Public Property ShareCount As Long
	Public Property CoverImgIdStr As String
	Public Property CommentCount As Long
End Class

Public Class User
	Public Property DefaultAvatar As Boolean
	Public Property Province As Long
	Public Property AuthStatus As Long
	Public Property Followed As Boolean
	Public Property AvatarUrl As String
	Public Property AccountStatus As Long
	Public Property Gender As Long
	Public Property City As Long
	Public Property Birthday As Long
	Public Property UserId As Long
	Public Property UserType As Long
	Public Property Nickname As String
	Public Property Signature As String
	Public Property Description As String
	Public Property DetailDescription As String
	Public Property AvatarImgId As Long
	Public Property BackgroundImgId As Long
	Public Property BackgroundUrl As String
	Public Property Authority As Long
	Public Property Mutual As Boolean
	Public Property ExpertTags As Object
	Public Property Experts As Object
	Public Property DjStatus As Long
	Public Property VipType As Long
	Public Property RemarkName As Object
	Public Property BackgroundImgIdStr As String
	Public Property AvatarImgIdStr As String
End Class

Public Class CloudMusicTrack
	Public Property Name As String
	Public Property Id As Long
	Public Property Pst As Long
	Public Property T As Long
	Public Property Ar As Ar()
	Public Property Alia As String()
	Public Property Pop As Double
	Public Property St As Long
	Public Property Rt As String
	Public Property Fee As Long
	Public Property V As Long
	Public Property Crbt As String
	Public Property Cf As String
	Public Property Al As Al
	Public Property Dt As Long
	Public Property H As H
	Public Property M As H
	Public Property L As H
	Public Property A As Object
	Public Property Cd As String
	Public Property No As Long
	Public Property RtUrl As Object
	Public Property Ftype As Long
	Public Property RtUrls As Object()
	Public Property DjId As Long
	Public Property Copyright As Long
	Public Property SId As Long
	Public Property Mst As Long
	Public Property Cp As Long
	Public Property Mv As Long
	Public Property Rtype As Long
	Public Property Rurl As Object
	Public Property PublishTime As Long
	Public Property Tns As String()
End Class

Public Class TrackId
	Public Property Id As Long
	Public Property V As Long
End Class

Public Class Datum
	Public Property Id As Long
	Public Property Url As String
	Public Property Br As Long
	Public Property Size As Long
	Public Property Md5 As String
	Public Property Code As Long
	Public Property Expi As Long
	Public Property Type As String
	Public Property Gain As Double
	Public Property Fee As Long
	Public Property Uf As Object
	Public Property Payed As Long
	Public Property Flag As Long
	Public Property CanExtend As Boolean
End Class

Public Class SearchResult
	Public Property Result As SResult
	Public Property Code As Long
End Class

Public Class ArtistResult
	Public Property Code As Long
	Public Property Artist As Artist
	Public Property More As Boolean
	Public Property HotSongs As List(Of HotSong)
End Class

Public Class DetailResult
	Public Property Songs As Song()
	Public Property Privileges As Privilege()
	Public Property Code As Long
End Class

Public Class Artist
	Public Property Img1V1Id As Long
	Public Property TopicPerson As Long
	Public Property PicId As Long
	Public Property BriefDesc As Object
	Public Property AlbumSize As Long
	Public Property Img1V1Url As String
	Public Property PicUrl As String
	Public Property [Alias] As List(Of String)
	Public Property Trans As String
	Public Property MusicSize As Long
	Public Property Name As String
	Public Property Id As Long
	Public Property PublishTime As Long
	Public Property MvSize As Long
	Public Property Followed As Boolean
End Class

Public Class AlbumResult
	Public Property Songs As Song()
	Public Property Code As Long
	Public Property Album As Album
End Class

Public Class Album
	Public Property Songs As Object()
	Public Property Paid As Boolean
	Public Property OnSale As Boolean
	Public Property PicId As Long
	Public Property [Alias] As Object()
	Public Property CommentThreadId As String
	Public Property PublishTime As Long
	Public Property Company As String
	Public Property CopyrightId As Long
	Public Property PicUrl As String
	Public Property Artist As Artist
	Public Property BriefDesc As Object
	Public Property Tags As String
	Public Property Artists As Artist()
	Public Property Status As Long
	Public Property Description As Object
	Public Property SubType As Object
	Public Property BlurPicUrl As String
	Public Property CompanyId As Long
	Public Property Pic As Long
	Public Property Name As String
	Public Property Id As Long
	Public Property Type As String
	Public Property Size As Long
	Public Property PicIdStr As String
	Public Property Info As Info
End Class

Public Class Info
	Public Property CommentThread As CommentThread
	Public Property LatestLikedUsers As Object
	Public Property Liked As Boolean
	Public Property Comments As Object
	Public Property ResourceType As Long
	Public Property ResourceId As Long
	Public Property CommentCount As Long
	Public Property LikedCount As Long
	Public Property ShareCount As Long
	Public Property ThreadId As String
End Class

Public Class CommentThread
	Public Property Id As String
	Public Property ResourceInfo As ResourceInfo
	Public Property ResourceType As Long
	Public Property CommentCount As Long
	Public Property LikedCount As Long
	Public Property ShareCount As Long
	Public Property HotCount As Long
	Public Property LatestLikedUsers As Object
	Public Property ResourceId As Long
	Public Property ResourceOwnerId As Long
	Public Property ResourceTitle As String
End Class

Public Class ResourceInfo
	Public Property Id As Long
	Public Property UserId As Long
	Public Property Name As String
	Public Property ImgUrl As Object
	Public Property Creator As Object
End Class

Public Class HotSong
	Public Property RtUrls As List(Of Object)
	Public Property Ar As List(Of Ar)
	Public Property Al As Al
	Public Property St As Long
	Public Property Fee As Long
	Public Property Ftype As Long
	Public Property Rtype As Long
	Public Property Rurl As Object
	Public Property T As Long
	Public Property Cd As String
	Public Property No As Long
	Public Property V As Long
	Public Property A As Object
	Public Property M As H
	Public Property DjId As Long
	Public Property Crbt As Object
	Public Property RtUrl As Object
	Public Property Alia As List(Of Object)
	Public Property Pop As Long
	Public Property Rt As String
	Public Property Mst As Long
	Public Property Cp As Long
	Public Property Cf As String
	Public Property Dt As Long
	Public Property Pst As Long
	Public Property H As H
	Public Property L As H
	Public Property Mv As Long
	Public Property Name As String
	Public Property Id As Long
	Public Property Privilege As Privilege
End Class

Public Class SResult
	Public Property Songs As List(Of Song)
	Public Property SongCount As Long
End Class

Public Class Song
	Public Property Name As String
	Public Property Id As Long
	Public Property Pst As Long
	Public Property T As Long
	Public Property Ar As List(Of Ar)
	Public Property Alia As List(Of Object)
	Public Property Pop As Long
	Public Property St As Long
	Public Property Rt As String
	Public Property Fee As Long
	Public Property V As Long
	Public Property Crbt As Object
	Public Property Cf As String
	Public Property Al As Al
	Public Property Dt As Long
	Public Property H As H
	Public Property M As H
	Public Property L As H
	Public Property A As Object
	Public Property Cd As String
	Public Property No As Long
	Public Property RtUrl As Object
	Public Property Ftype As Long
	Public Property RtUrls As List(Of Object)
	Public Property Rurl As Object
	Public Property Rtype As Long
	Public Property Mst As Long
	Public Property Cp As Long
	Public Property Mv As Long
	Public Property PublishTime As Long
	Public Property Privilege As Privilege
End Class

Public Class Al
	Public Property Id As Long
	Public Property Name As String
	Public Property PicUrl As String
	Public Property Tns As List(Of Object)
	Public Property Pic As Long
End Class

Public Class Ar
	Public Property Id As Long
	Public Property Name As String
	Public Property Tns As List(Of Object)
	Public Property [Alias] As List(Of Object)
End Class

Public Class H
	Public Property Br As Long
	Public Property Fid As Long
	Public Property Size As Long
	Public Property Vd As Double
End Class

Public Class Privilege
	Public Property Id As Long
	Public Property Fee As Long
	Public Property Payed As Long
	Public Property St As Long
	Public Property Pl As Long
	Public Property Dl As Long
	Public Property Sp As Long
	Public Property Cp As Long
	Public Property Subp As Long
	Public Property Cs As Boolean
	Public Property Maxbr As Long
	Public Property Fl As Long
	Public Property Toast As Boolean
	Public Property Flag As Long
End Class