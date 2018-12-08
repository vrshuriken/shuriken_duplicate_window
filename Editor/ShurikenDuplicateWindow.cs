using UnityEngine;
using UnityEditor;

// 処理上の理由で追加
using System.Text.RegularExpressions;
using System.Collections.Generic;


/**
 * Window> Shuriken Duplicate >　諸々設定 > オブジェクト作成 押下でオブジェクトを等間隔で作成するUnityエディタ拡張
 * Hierarchy上で2オブジェクト以上選択した状態ならそれの並べ方で作成する
 * This File licensed under the terms of the MIT license
 * Copyright (c) 2018 vrshuriken
 * <version>1.0.0</version>
 */
class ShurikenDuplicate : EditorWindow
{
    /**
     * 選択したオブジェクト
     */
    GameObject[] sortedSelectedGameObjects = new GameObject[0];

    /**
     * オブジェクト生成の設定値
     */
    string makeObjectName = "New GameObject";
    int makeObjectIndex = 0;
    Vector3 makeObjectPos;
    Vector3 makeObjectScale = new Vector3(1.0f, 1.0f, 1.0f);
    Vector3 makeObjectRotate;
    GameObject selectedParentObject;
    GameObject selectedFirstObject;
    int makeObjectNum = 1;
    bool isAlsoUpdateSelectedObject = true;


    /**
     * オブジェクトを規則的に並べるための設定値
     * slideVectorAをslideNumA回足す -> slideVectorBをslideNumB回足す -> slideVectorAをslideNumA回足す->...の繰り返しで並べる
     * デフォルトで横一列に並べる
     */
    Vector3 slideVectorA = new Vector3(1.0f,0,0); // オブジェクトをスライドさせるベクトルA
    Vector3 slideVectorB;
    int slideNumA = 1; // objectSlideAを連続適用する回数
    int slideNumB;

    bool isInitialized = false;
    bool showAdvancedSetting = false; // falseだと設定できる項目を減らしてUIをシンプルにする
    string logPrefix = "";

    public enum OBJ_TYPE
    {
        SAME = 0,
        EMPTY = 1,
        CUBE = 2
    }
    public OBJ_TYPE objType;

    // windowにメニュー追加します。EditorWindowを継承したクラス・MenuItemのプロパティ、OnGUIメソッドでセット。
    [MenuItem("Window/Shuriken Duplicate")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(ShurikenDuplicate));　// OnGUI()が呼ばれます
    }

    /**
     * GUIを表示します
     */
    void OnGUI()
    {
        logPrefix = "[" + this.GetType().Name + "] ";

        // ヒエラルキーでオブジェクトを選択していた場合は初期値をいい感じにセットしておく
        // 規則的に並べたオブジェクトを2個以上選択していた場合は、同じように並ぶように初期値をセットする
        if (!isInitialized && Selection.gameObjects != null && 2 <= Selection.gameObjects.Length)
        {
            SetInitValues(Selection.gameObjects);
            isInitialized = true;
        }
        else　if (!isInitialized)
        {
            UnityEditor.EditorUtility.DisplayDialog("Advancedモードで開きます", "通常モードはHierarchyで2オブジェクト以上選択してから「Shuriken Duplicate」を開きなおしてください", "OK");
            showAdvancedSetting = true;
            isInitialized = true;　
        }

        
        GUIStyle bold = new GUIStyle()
        {
            //alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
        };

        EditorGUILayout.LabelField("オブジェクト配置", bold);
        selectedParentObject = EditorGUILayout.ObjectField("親", selectedParentObject, typeof(GameObject), true) as GameObject;
        makeObjectNum = EditorGUILayout.IntField("配置数(選択中含め)", makeObjectNum);
                                                                              
        if (showAdvancedSetting) makeObjectIndex = EditorGUILayout.IntField("何個目から作成するか", makeObjectIndex);

        if (showAdvancedSetting) slideVectorA = EditorGUILayout.Vector3Field("配置間隔", slideVectorA);
        if (showAdvancedSetting) slideNumA = EditorGUILayout.IntField("連続回数", slideNumA);

        if (showAdvancedSetting)
        {
            EditorGUILayout.LabelField("ジグザグ配置用(連続回数を1以上にすると有効)", bold);
            slideVectorB = EditorGUILayout.Vector3Field("配置間隔", slideVectorB);
            slideNumB = EditorGUILayout.IntField("連続回数", slideNumB);
        }
        


        EditorGUILayout.Space();


        EditorGUILayout.LabelField("作成するオブジェクト", bold);
        makeObjectName = EditorGUILayout.TextField("名前(自動連番)", makeObjectName);
        if (showAdvancedSetting) makeObjectPos = EditorGUILayout.Vector3Field("初期Position", makeObjectPos);
        makeObjectScale = EditorGUILayout.Vector3Field("Scale", makeObjectScale);
        makeObjectRotate = EditorGUILayout.Vector3Field("Rotate", makeObjectRotate);
        objType = (OBJ_TYPE)EditorGUILayout.EnumPopup("オブジェクトの種類", objType);


        EditorGUILayout.Space();

        if (sortedSelectedGameObjects.Length > 0)
        {
            isAlsoUpdateSelectedObject = EditorGUILayout.Toggle("選択オブジェクト更新", isAlsoUpdateSelectedObject);
        }

        EditorGUILayout.Space();


        // ボタンをGUIに配置、ボタン押下でオブジェクト作成する
        if (GUILayout.Button("オブジェクト作成"))
        {
            Make();
        }
 

    }

    /**
     * 選択したオブジェクトを見て並べ方などの設定の初期値を決めます
     */
    private void SetInitValues(GameObject[] selectedGameObjects)
    {
        // 選択したオブジェクトはヒエラルキー上の並び順で処理する
        sortedSelectedGameObjects = new GameObject[selectedGameObjects.Length];
        var itemTable = new SortedDictionary<int, GameObject>();
        foreach (GameObject obj in selectedGameObjects)
        {
            itemTable.Add(obj.transform.GetSiblingIndex(), obj);

        }
        itemTable.Values.CopyTo(sortedSelectedGameObjects, 0);


        const int INDEX_OF_A = 0;
        const int INDEX_OF_B = 1;
        const int INDEX_OF_C = 2;
        Vector3[] vecPos = new Vector3[3];// ベクトルAの始点,ベクトルAの終点 & ベクトルBの始点,ベクトルBの終点
        bool[] isSetVecPos = new bool[3];
        int[] vecIndex = new int[3]; // それぞれのベクトルの始点終点が見つかったindex
        bool isLastDiffPos = false;
        Quaternion lastDiffPos = new Quaternion();
        GameObject lastObj = null;

        for (int i = 0; i < sortedSelectedGameObjects.Length; i++)
        {

            GameObject obj = sortedSelectedGameObjects[i];
            Vector3 pos = obj.transform.localPosition;

            // 最初のオブジェクトと同じものを複製するので記憶しておく
            if (i==0)
            {
                selectedFirstObject = obj;
            }

            // 選択したオブジェクトの並び方をジグザグの二本のベクトルで表現する
            // x,y,z軸の値の変化を見ていずれかの軸で折り返したら次のベクトルの開始と判断する

            
            if (i == 0)
            {
                makeObjectPos = pos; // 選択オブジェクトと同じ並び方にしたいのでオブジェクトを作る起点は最初のオブジェクトの位置
                selectedParentObject = obj.transform.parent.gameObject; // 選択オブジェクトと共通の親になるように作成したい

                vecIndex[INDEX_OF_A] = i;
                vecPos[INDEX_OF_A] = obj.transform.localPosition; // ベクトルAの始点
                isSetVecPos[INDEX_OF_A] = true;
            }

            // 並びが折り返しているか調べ、ベクトルAの終点 & ベクトルBの始点,ベクトルBの終点を見つける
            if (lastObj != null)
            {
                Quaternion diffPos = Quaternion.Euler(obj.transform.localPosition - lastObj.transform.localPosition);
                if (isLastDiffPos)
                {
                    float angle = Quaternion.Angle(diffPos, lastDiffPos);
                    if (angle >= 0.15) // 値は適当 折り返し
                    {
                        if (!isSetVecPos[INDEX_OF_B])
                        {
                            vecIndex[INDEX_OF_B] = i-1;
                            vecPos[INDEX_OF_B] = lastObj.transform.localPosition; // ベクトルAの終点 & ベクトルBの始点
                            isSetVecPos[INDEX_OF_B] = true;
                        }
                        else if (!isSetVecPos[INDEX_OF_C])
                        {
                            vecIndex[INDEX_OF_C] = i-1;
                            vecPos[INDEX_OF_C] = lastObj.transform.localPosition; // ベクトルBの終点
                            isSetVecPos[INDEX_OF_C] = true;
                        }
                    }
                }
                lastDiffPos = diffPos;
                isLastDiffPos = true;
            }

            // 最後
            if (i + 1 >= sortedSelectedGameObjects.Length)
            {
                // 折り返しが最後までなければ最後の場所を折り返しと判断
                if (!isSetVecPos[INDEX_OF_B]) // この場合はベクトルAしかない
                {
                    vecIndex[INDEX_OF_B] = i;
                    vecPos[INDEX_OF_B] = pos; // ベクトルAの終点 & ベクトルBの始点
                    isSetVecPos[INDEX_OF_B] = true;

                    vecIndex[INDEX_OF_C] = i;
                    vecPos[INDEX_OF_C] = pos; // ベクトルBの終点
                    isSetVecPos[INDEX_OF_C] = true;
                }
                else if (!isSetVecPos[INDEX_OF_C])
                {
                    vecIndex[INDEX_OF_C] = i;
                    vecPos[INDEX_OF_C] = pos; // ベクトルBの終点
                    isSetVecPos[INDEX_OF_C] = true;
                }


                makeObjectIndex = i + 1; // 選択したオブジェクトの並びの最後から続けて作成したい
                makeObjectNum = makeObjectIndex + 1; // 最低一個作成するように初期値を設定
            }

            lastObj = obj;
        }

        // 並べ方が決まる
        slideNumA = vecIndex[INDEX_OF_B] - vecIndex[INDEX_OF_A];
        slideNumB = vecIndex[INDEX_OF_C] - vecIndex[INDEX_OF_B];
        if (slideNumA == 0)
        {
            slideVectorA = new Vector3(0f, 0f, 0f);
        }
        else
        {
            slideVectorA = (vecPos[INDEX_OF_B] - vecPos[INDEX_OF_A]) / slideNumA;
        }
        if (slideNumB == 0)
        {
            slideVectorB = new Vector3(0f, 0f, 0f);
        }
        else
        {
            slideVectorB = (vecPos[INDEX_OF_C] - vecPos[INDEX_OF_B]) / slideNumB;
        }

        // 最初のオブジェクトと同じ名前(連番)で複製するイメージ
        Regex reg = new Regex(@" \((?<number>[0-9]+)\)");
        string name = reg.Replace(selectedFirstObject.name, "");
        makeObjectName = name;

        // 最初のオブジェクトと同じ設定で複製する
        makeObjectScale = selectedFirstObject.transform.localScale;
        makeObjectRotate = selectedFirstObject.transform.localRotation.eulerAngles;

        Debug.Log(logPrefix + "選択したオブジェクトをもとに初期値をセットしました");
    }

    /**
     * 設定とオブジェクトのindexをもとに配置を決定する
     */
    private Vector3 CalcPosition(int index)
    {
        // 規則的にオブジェクトを並べたい
        int cycleNum = 0;
        int surplus = 0;
        if (slideNumA + slideNumB != 0) // 0除算エラー回避
        {
            cycleNum = index / (slideNumA + slideNumB);
            surplus = index % (slideNumA + slideNumB);
        }
        int surplusA;
        int surplusB;
        if (surplus > slideNumA)
        {
            surplusA = slideNumA;
            surplusB = surplus - slideNumA;
        }
        else
        {
            surplusA = surplus;
            surplusB = 0;
        }
        int addANum = (cycleNum * slideNumA) + surplusA;
        int addBNum = (cycleNum * slideNumB) + surplusB;
        Vector3 pos = makeObjectPos + slideVectorA * addANum + slideVectorB * addBNum;
        return pos;
    }

    /**
     * ベースの名前とオブジェクトのindexをもとにUnityぽい命名規則の名前を決定する
     * ex. hoge, hoge (1), hoge (2)
     */
    private string CalcName(string makeObjectName, int index)
    {
        string objectName = makeObjectName;
        if (index != 0)
        {
            objectName += " (" + index + ")";
        }
        return objectName;
    }

    /**
     * 設定をもとにオブジェクトを生成して並べます
     */
    private void Make()
    {
        // 入力値チェック
        if (slideNumA < 0 || slideNumB < 0)
        {
            UnityEditor.EditorUtility.DisplayDialog("修正してください", "連続回数は整数を入力してください", "OK");
            return;
        }
        if (makeObjectNum < 0)
        {
            UnityEditor.EditorUtility.DisplayDialog("修正してください", "配置数は整数を入力してください", "OK");
            return;
        }
        if (makeObjectNum <= makeObjectIndex)
        {
            UnityEditor.EditorUtility.DisplayDialog("修正してください", "配置数は「何個目から作成するか」より大きい数字を入力してください", "OK");
            return;
        }
        if (sortedSelectedGameObjects.Length == 0 && objType == OBJ_TYPE.SAME)
        {
            UnityEditor.EditorUtility.DisplayDialog("修正してください", "オブジェクトがされていないのでSAMEは選べません", "OK");
            return;
        }

        // 選択したオブジェクトも配置を綺麗し同じ設定にする
        if (isAlsoUpdateSelectedObject && sortedSelectedGameObjects.Length > 0)
        {
            for (int i=0; i<sortedSelectedGameObjects.Length; i++)
            {
                Undo.RecordObject(sortedSelectedGameObjects[i].transform, "Update Selected GameObject transform"); 
                Undo.RecordObject(sortedSelectedGameObjects[i], "Update Selected GameObject");
                sortedSelectedGameObjects[i].transform.localPosition = CalcPosition(i);
                sortedSelectedGameObjects[i].transform.localScale = makeObjectScale;
                sortedSelectedGameObjects[i].transform.localRotation = Quaternion.Euler(makeObjectRotate);
                sortedSelectedGameObjects[i].name = CalcName(makeObjectName, i);
            }
        }

        // 新しくオブジェクト作成
        for (int i = makeObjectIndex; i < makeObjectNum; i++) { // UIで入力した数だけ作ります

            // 名前をUnityぽい命名規則にしておく
            string objectName = CalcName(makeObjectName, i);

            //新しいゲームオブジェクトを作成、その事をUndoに記録
            GameObject newGameObject;
            if (objType == OBJ_TYPE.SAME)
            {
                newGameObject = GameObject.Instantiate(selectedFirstObject);
                newGameObject.name = objectName;
            }
            else if (objType == OBJ_TYPE.CUBE)
            {
                newGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                newGameObject.name = objectName;
            }
            else // OBJ_TYPE.EMPTY
            {
                newGameObject = new GameObject(objectName);
            }
            Undo.RegisterCreatedObjectUndo(newGameObject, "Create New GameObject");

            // 選択したオブジェクトの子に配置する
            // localPositionを更新する前にこれをやらないと、ワールド配置->localPositionセット->配置 でlocalPositionが変わってしまう
            if (selectedParentObject != null)
            {
                newGameObject.transform.parent = selectedParentObject.transform;
            }


            newGameObject.transform.localPosition = CalcPosition(i); // 規則的にオブジェクトを並べたい
            newGameObject.transform.localScale = makeObjectScale;
            newGameObject.transform.localRotation = Quaternion.Euler(makeObjectRotate); 
        }

        Debug.Log(logPrefix + "オブジェクトを作成しました");
    }
}