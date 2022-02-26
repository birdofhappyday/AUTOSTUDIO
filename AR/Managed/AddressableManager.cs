using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Managed;
using System;
using UnityEngine.ResourceManagement.ResourceLocations;

// 번들 타입 종류
// 서버와 인덱스가 동일해야한다.
// 마지막은 타입의 개수 체크를 위해서 배치.
public enum BundleType
{
    NONE,
    MAP,
    AVATAR,
    EMPHASIS,
    EXPRESSION,
    COUNT,
}

public class AddressableManager : ARPFunction.MonoBeHaviourSingleton<AddressableManager>
{
    [Serializable]
    public class BundleInfo
    {
        public BundleType bundleType;
        public string name;
    }

    public bool IsLocal = false;

    //[김성민] 서버에서 받은 정보를 리스트에 채워준다.
    private Dictionary<BundleType, List<string>> bundleDic = new Dictionary<BundleType, List<string>>();
    private List<string> bundleKeyword = new List<string>();


    [SerializeField]
    private List<BundleInfo> localBundleInfoList;

    private void Awake()
    {
        DontDestroyOnLoad(this);

        if (IsLocal)
        {
            foreach (BundleInfo _bundleInfo in localBundleInfoList)
                AddBundleInfos(_bundleInfo.bundleType, _bundleInfo.name);
        }
    }

    // 해당 번들타입에 목록을 채운다.
    public void AddBundleInfos(BundleType _bundleType, string _bundleName)
    {
        List<string> _values = GetBundleNameList(_bundleType);
        _values.Add(_bundleName);
        bundleDic[_bundleType] = _values;
    }

    // 해당 번들타입에 해당되는 목록을 반환한다.
    // 해당 타입이 빈 경우에는 빈 리스트를 반환한다.
    public List<string> GetBundleNameList(BundleType _bundleType)
    {
        List<string> _result;
        bundleDic.TryGetValue(_bundleType, out _result);
        if (_result == null)
            _result = new List<string>();

        return _result;
    }

    // 모든 번들의 이름을 리스트로 반환한다.
    public List<string> GetAllBundleNameList()
    {
        List<string> _result;
        _result = GetBundleNameList(0);

        for (int i = 1; i < (int)BundleType.COUNT; ++i)
        {
            _result.AddRange(GetBundleNameList((BundleType)i));
        }
        return _result;
    }

    // 번들의 이름을 초기화
    public void ClearBundleInfo()
    {
        bundleDic.Clear();
    }

    // 모든 번들의 개수를 반환한다.
    public int CountBundleDic()
    {
        return GetAllBundleNameList().Count;
    }

    // 각 번들 타입의 개수에 대해서 반환해준다.
    public int CountBundleDic(BundleType _bundleType)
    {
        return GetBundleNameList(_bundleType).Count;
    }

    // 해당 번들을 가지고 있는지 판단한다.
    public bool HaveBundleName(BundleType _bundleType, string _bundleName)
    {
        return GetBundleNameList(_bundleType).Contains(_bundleName);
    }

    // Bundle Load
    // 필요한 번들 수집
    public void BundleInfoCollect(string project, string flatform)
    {
        if (AddressableResourceExists(project))
            bundleKeyword.Add(project);

        if (AddressableResourceExists(flatform))
            bundleKeyword.Add(flatform);

        if (AddressableResourceExists(string.Format("{0}_{1}", project, flatform)))
            bundleKeyword.Add(string.Format("{0}_{1}", project, flatform));

        foreach (string _bundleName in GetAllBundleNameList())
        {
            if (AddressableResourceExists(_bundleName))
            {
                bundleKeyword.Add(_bundleName);
            }
        }

        BundleSizeCheck();
    }

    //[김성민] 어드레서블 키를 가진 오브젝트가 있는지 체크해서 추가할지 결정한다.(체크하지 않고 진행할경우 오브젝트가 없기 때문에 에러를 뱉는다.)
    public bool AddressableResourceExists(object key)
    {
        foreach (var l in Addressables.ResourceLocators)
        {
            IList<IResourceLocation> locs;
            if (l.Locate(key, null, out locs))
                return true;
        }
        return false;
    }


    // Bundle Size Check
    // 번들 사이즈 체크
    public void BundleSizeCheck()
    {
        Addressables.GetDownloadSizeAsync(bundleKeyword).Completed += (AsyncOperationHandle<long> sizeHandle) =>
        {
            while (!sizeHandle.IsDone)
                continue;

            if (sizeHandle.Status == AsyncOperationStatus.Succeeded)
            {
                string sizeTest = string.Concat(sizeHandle.Result, " byte");
                Debug.Log("다운로드 파일 크기 : " + sizeTest);
                BundleDownload();
                Addressables.Release(sizeHandle);
            };
        };
    }

    // Bundle Download
    public void BundleDownload()
    {
        AsyncOperationHandle downHandle = Addressables.DownloadDependenciesAsync(bundleKeyword, Addressables.MergeMode.Union);
        downHandle.Completed += DownloadCompleted;
        Addressables.Release(downHandle);
    }

    private void DownloadCompleted(AsyncOperationHandle obj)
    {
        Debug.Log("Bundle Download Completed");

        //[김성민] 번들 다운로드가 끝나고 씬 전환을 진행한다.
        if (XRHubSceneManager.Instance.eCurScene == SceneState.LOGIN)
        {
#if REALCONNECT
            XRHubSceneManager.Instance.SceneChange(SceneState.LOBBY);
#else
            ResourceManager.Instance.DownloadCompleted();
#endif
        }
    }

    public void BundleLoadCompleted(IList<GameObject> assets)
    {
        List<GameObject> objs = new List<GameObject>();

        objs.AddRange(assets);
    }

    public void BundleLoadAssets<T>(T loadAssetType, List<string> loadAssetList, Action completedMethod = null)
    {

    }

    public void BundleLoadAssets<T>(T loadAssetList, Action completedMethod = null)
    {

    }

    // Bundle Release
    public void BundleRelease(GameObject obj)
    {
        Addressables.ReleaseInstance(obj);
    }

    public void BundleRelease<T>(T obj)
    {
        Addressables.Release(obj);
    }
}
