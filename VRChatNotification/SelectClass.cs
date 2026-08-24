namespace VRChatNotification
{
    internal class SelectClass
    {
        public SelectType SelectPublic { get; set; } = SelectType.NoSound;
        public SelectType SelectGroupPublic { get; set; } = SelectType.NoSound;
        public SelectType SelectGroupPlus { get; set; } = SelectType.NoSound;
        public SelectType SelectGroup { get; set; } = SelectType.NoSound;
        public SelectType SelectHidden { get; set; } = SelectType.NoSound;
        public SelectType SelectFriends { get; set; } = SelectType.NoSound;
        public SelectType SelectPrivatePlus { get; set; } = SelectType.NoSound;
        public SelectType SelectPrivate { get; set; } = SelectType.NoSound;
        public SelectType SelectUnknown { get; set; } = SelectType.NoSound;
        public int CurrentVolume { get; set; } = 100;
    }
}
