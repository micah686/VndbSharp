﻿using System;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using VndbSharp.Attributes;
using VndbSharp.Models.Common;

namespace VndbSharp.Models.VisualNovel
{
	public class VisualNovel
	{
		private VisualNovel() { }

		public UInt32 Id { get; private set; }
		[JsonProperty("title")]
		public String Name { get; private set; }
		[JsonProperty("original")]
		public String OriginalName { get; private set; }
		public SimpleDate Released { get; private set; } // Release or Released?
		public ReadOnlyCollection<String> Languages { get; private set; }
		[JsonProperty("orig_lang")]
		public ReadOnlyCollection<String> OriginalLanauges { get; private set; }
		public ReadOnlyCollection<String> Platforms { get; private set; }
		[IsCsv]
		public ReadOnlyCollection<String> Aliases { get; private set; }
		public VisualNovelLength? Length { get; private set; }
		public String Description { get; private set; }
		public VisualNovelLinks VisualNovelLinks { get; private set; }
		public String Image { get; private set; }
		[JsonProperty("image_nsfw")]
		public Boolean IsImageNsfw { get; private set; }
		public AnimeMetadata[] Anime { get; private set; }
		public VisualNovelRelation[] Relations { get; private set; }
		public TagMetadata[] Tags { get; private set; }
		public Single Popularity { get; private set; }
		public UInt32 Rating { get; private set; }
		[JsonProperty("screens")]
		public ScreenshotMetadata[] Screenshots { get; private set; }
	}
}