// 씬을 넘어서 유지되는 UI 가, 새 씬의 플레이어 등 씬 오브젝트를 다시 찾아야 할 때 구현한다.
// PersistentUiRoot 가 씬이 로드될 때마다 자식들의 이 메서드를 불러준다.
public interface ISceneRebindable
{
    void RebindSceneReferences();
}
