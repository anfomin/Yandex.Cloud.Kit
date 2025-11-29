namespace Yandex.Cloud;

/// <summary>
/// Voice for speech synthesis.
/// </summary>
public class Voice
{
	/// <summary>
	/// Gets voice key.
	/// </summary>
	public string Key { get;  }

	/// <summary>
	/// Gets voice role.
	/// </summary>
	public Role? RoleType { get; }

	private Voice(string key, Role? role = null)
		=> (Key, RoleType) = (key, role);

	/// <summary>
	/// Voice role for speech synthesis.
	/// </summary>
	public enum Role
	{
		Neutral,
		Good,
		Evil,
		Friendly,
		Strict,
		Whisper,
		Classic
	}

	/// <summary>
	/// Provides Russian voices.
	/// </summary>
	public static class Russian
	{
		public static readonly Voice Alena = new("alena");
		public static readonly Voice AlenaGood = new("alena", Role.Good);
		public static readonly Voice Filipp = new("filipp");
		public static readonly Voice Ermil = new("ermil");
		public static readonly Voice ErmilGood = new("ermil", Role.Good);
		public static readonly Voice Jane = new("jane");
		public static readonly Voice JaneGood = new("jane", Role.Good);
		public static readonly Voice JaneEvil = new("jane", Role.Evil);
		public static readonly Voice Omazh = new("omazh");
		public static readonly Voice OmazhEvil = new("omazh", Role.Evil);
		public static readonly Voice Zahar = new("zahar");
		public static readonly Voice ZaharGood = new("zahar", Role.Good);
		public static readonly Voice Dasha = new("dasha");
		public static readonly Voice DashaGood = new("dasha", Role.Good);
		public static readonly Voice DashaFriendly = new("dasha", Role.Friendly);
		public static readonly Voice Julia = new("julia");
		public static readonly Voice JuliaStrict = new("julia", Role.Strict);
		public static readonly Voice Lera = new("lera");
		public static readonly Voice LeraFriendly = new("lera", Role.Friendly);
		public static readonly Voice MashaGood = new("masha", Role.Good);
		public static readonly Voice MashaStrict = new("masha", Role.Strict);
		public static readonly Voice MashaFriendly = new("masha", Role.Friendly);
		public static readonly Voice Marina = new("marina");
		public static readonly Voice MarinaWhisper = new("marina", Role.Whisper);
		public static readonly Voice MarinaFriendly = new("marina", Role.Friendly);
		public static readonly Voice Alexander = new("alexander");
		public static readonly Voice AlexanderGood = new("alexander", Role.Good);
		public static readonly Voice Kirill = new("kirill");
		public static readonly Voice KirillStrict = new("kirill", Role.Strict);
		public static readonly Voice KirillGood = new("kirill", Role.Good);
		public static readonly Voice Anton = new("anton");
		public static readonly Voice AntonGood = new("anton", Role.Good);
		public static readonly Voice Madi = new("madi_ru");
		public static readonly Voice Saule = new("saule_ru");
		public static readonly Voice SauleStrict = new("saule_ru", Role.Strict);
		public static readonly Voice SauleWhisper = new("saule_ru", Role.Whisper);
		public static readonly Voice Zamira = new("zamira_ru");
		public static readonly Voice ZamiraStrict = new("zamira_ru", Role.Strict);
		public static readonly Voice ZamiraFriendly = new("zamira_ru", Role.Friendly);
		public static readonly Voice Zhanar = new("zhanar_ru");
		public static readonly Voice ZhanarStrict = new("zhanar_ru", Role.Strict);
		public static readonly Voice ZhanarFriendly = new("zhanar_ru", Role.Friendly);
		public static readonly Voice Yulduz = new("yulduz_ru");
		public static readonly Voice YulduzStrict = new("yulduz_ru", Role.Strict);
		public static readonly Voice YulduzFriendly = new("yulduz_ru", Role.Friendly);
		public static readonly Voice YulduzWhisper = new("yulduz_ru", Role.Whisper);
	}

	/// <summary>
	/// Provides English voices.
	/// </summary>
	public static class English
	{
		public static readonly Voice John = new("john");
	}

	/// <summary>
	/// Provides German voices.
	/// </summary>
	public static class German
	{
		public static readonly Voice Lea = new("lea");
	}

	/// <summary>
	/// Provides Hebrew voices.
	/// </summary>
	public static class Hebrew
	{
		public static readonly Voice NaomiModern = new("naomi");
		public static readonly Voice NaomiClassic = new("naomi", Role.Classic);
	}

	/// <summary>
	/// Provides Kazakh voices.
	/// </summary>
	public static class Kazakh
	{
		public static readonly Voice Amira = new("amira");
		public static readonly Voice Madi = new("madi");
		public static readonly Voice Saule = new("saule");
		public static readonly Voice SauleStrict = new("saule", Role.Strict);
		public static readonly Voice Zhanar = new("zhanar");
		public static readonly Voice ZhanarFriendly = new("zhanar", Role.Friendly);
	}

	/// <summary>
	/// Provides Uzbek voices.
	/// </summary>
	public static class Uzbek
	{
		public static readonly Voice Amira = new("nigora");
		public static readonly Voice Madi = new("zamira");
		public static readonly Voice MadiStrict = new("zamira", Role.Strict);
		public static readonly Voice MadiFriendly = new("zamira", Role.Friendly);
		public static readonly Voice Saule = new("yulduz");
		public static readonly Voice SauleStrict = new("yulduz", Role.Strict);
		public static readonly Voice SauleFriendly = new("yulduz", Role.Friendly);
		public static readonly Voice SauleWhisper = new("yulduz", Role.Whisper);
	}
}