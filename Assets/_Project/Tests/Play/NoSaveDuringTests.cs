using MemorialFloor.Game;
using NUnit.Framework;
using UnityEngine.SceneManagement;

//保存のファイルは枠ごとに 1 つしか無い。テストのあいだに読み書きすると、前のテストの状態が次のテストに持ち越され、
//エディタで遊んだときの保存も読み込んでしまう。名前空間の外に置いた SetUpFixture は、このアセンブリのテスト全体の前後に 1 度ずつ走る。
//常駐シーンを読み込むたびに、どの Start よりも前に保存を切る。切った常駐シーンは、再生を止めるときにも書かない
[SetUpFixture]
public class NoSaveDuringTests
{
    [OneTimeSetUp]
    public void 保存を止める()
    {
        SceneManager.sceneLoaded += TurnOff;
    }

    [OneTimeTearDown]
    public void 止めるのをやめる()
    {
        SceneManager.sceneLoaded -= TurnOff;
    }

    private static void TurnOff(Scene scene, LoadSceneMode mode)
    {
        SaveSystem save = SaveSystem.In(scene);
        if (save != null) save.TurnOff();
    }
}
