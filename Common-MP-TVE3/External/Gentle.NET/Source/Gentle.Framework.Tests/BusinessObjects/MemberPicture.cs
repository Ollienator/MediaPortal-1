/*
 * Test cases
 * Copyright (C) 2004 Clayton Harbour
 * 
 * This library is free software; you can redistribute it and/or modify it 
 * under the terms of the GNU Lesser General Public License 2.1 or later, as
 * published by the Free Software Foundation. See the included License.txt
 * or http://www.gnu.org/copyleft/lesser.html for details.
 *
 * $Id: MemberPicture.cs 1232 2008-03-14 05:36:00Z mm $
 */
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Gentle.Framework.Tests
{
	/// <summary>
	/// The MemberPicture class represents a MailingList subscriber picture.
	/// </summary>
	[TableName( "MemberPicture" )]
	public class MemberPicture : Persistent
	{
		private int pictureId;
		private byte[] picture;
		private int memberId;

		#region Constructors
		public MemberPicture( int id, byte[] pictureData, int memberId )
		{
			pictureId = id;
			picture = pictureData;
			this.memberId = memberId;
		}

		public MemberPicture( int id, Image pictureData, int memberId ) :
			this( id, ConvertToByteArray( pictureData ), memberId )
		{
		}

		public MemberPicture( Image pictureData, int memberId ) :
			this( 0, ConvertToByteArray( pictureData ), memberId )
		{
		}

		public MemberPicture( byte[] pictureData, int memberId ) :
			this( 0, pictureData, memberId )
		{
		}

		public static MemberPicture Retrieve( int id )
		{
			Key key = new Key( typeof(MemberPicture), true, "Id", id );
			return Broker.RetrieveInstance( typeof(MemberPicture), key ) as MemberPicture;
		}
		#endregion

		#region Properties
		[TableColumn( "PictureId" ), PrimaryKey( AutoGenerated = true ), SequenceName( "MEMBERPICTURE_SEQ" )]
		public int Id
		{
			get { return pictureId; }
			set { pictureId = value; }
		}

		[TableColumn( "PictureData", NotNull = false )]
		public byte[] PictureData
		{
			get { return picture; }
			set { picture = value; }
		}

		[TableColumn( "MemberId", NotNull = true ), ForeignKey( "ListMember", "MemberId" )]
		public int MemberId
		{
			get { return memberId; }
			set { memberId = value; }
		}

		public Image Picture
		{
			get { return ConvertToImage( picture ); }
		}

		public int Size
		{
			get { return picture.Length; }
		}
		#endregion

		#region Image converters
		protected static byte[] ConvertToByteArray( Image picture )
		{
			MemoryStream memoryStream = new MemoryStream();
			picture.Save( memoryStream, ImageFormat.Jpeg );
			return memoryStream.ToArray();
		}

		protected static Image ConvertToImage( byte[] pictureData )
		{
			return Image.FromStream( new MemoryStream( pictureData ) );
		}
		#endregion
	}
}