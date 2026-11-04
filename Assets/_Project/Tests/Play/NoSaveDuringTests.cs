using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

//保存のファイルは 1 つしか無い。テストのあいだに読み書きすると、前のテストの状態が次のテストに持ち越され、
//エディタで遊んだときの保存も読み込んでしまう。名前空間の外に置いた SetUpFixture は、このアセンブリのテスト全体の前に 1 度走る。
//終わったあとに戻さない。戻すと、再生を止めるときの書き込みがテストの状態を保存する。戻すのは次の再生の始め（SaveSystem.EnableOnPlay）
[SetUpFixture]
public class NoSaveDuringTests
{
    [OneTimeSetUp]
    public void 保存を止める()
    {
        Type type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("SaveSystem"))
            .FirstOrDefault(t => t != null);
        Assert.IsNotNull(type, "SaveSystem 型が見つからない");

        FieldInfo enabled = type.GetField("Enabled", BindingFlags.Public | BindingFlags.Static);
        Assert.IsNotNull(enabled, "SaveSystem.Enabled が見つからない");

        enabled.SetValue(null, false);
    }
}
