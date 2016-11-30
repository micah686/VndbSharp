﻿using System.ComponentModel;

namespace VndbSharp.Enums
{
	public enum VnRelation
	{
		Sequel,
		Prequel,
		[Description("Same Setting")]
		SameSetting,
		[Description("Alternative Version")]
		AlternativeVersion,
		[Description("Shares Characters")]
		SharesCharacters,
		[Description("Side Story")]
		SideStory,
		[Description("Parent Story")]
		ParentStory,
		[Description("SAme Series")]
		SameSeries,
		Fandisc,
		[Description("Original Game")]
		OriginalGame,
	}
}
