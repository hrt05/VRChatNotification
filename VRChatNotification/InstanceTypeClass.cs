namespace VRChatNotification
{
    internal class InstanceTypeClass
    {

        public InstanceType instanceTypeClassDef(string line)
        {
            // インバイト+
            if (line.Contains("private") && line.Contains("canRequestInvite"))
            {
                return InstanceType.PrivatePlus;
            }
            // インバイト
            else if (line.Contains("private"))
            {
                return InstanceType.Private;
            }
            // フレンド
            else if (line.Contains("friends"))
            {
                return InstanceType.Friends;
            }
            // フレンド+
            else if (line.Contains("hidden"))
            {
                return InstanceType.Hidden;
            }
            // グループ
            else if (line.Contains("group") && line.Contains("groupAccessType(members)"))
            {
                return InstanceType.Group;
            }
            // グループ+
            else if (line.Contains("group") && line.Contains("groupAccessType(plus)"))
            {
                return InstanceType.GroupPlus;
            }
            // グループパブリック
            else if (line.Contains("group") && line.Contains("groupAccessType(public)"))
            {
                return InstanceType.GroupPublic;
            }
            // パブリック
            else if (line.Contains("region"))
            {
                return InstanceType.Public;
            }
            else
            {
                return InstanceType.Unknown;
            }
        }
    }
}