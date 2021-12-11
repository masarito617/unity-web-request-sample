using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 本リスト管理クラス
/// 主にリクエスト関連のテスト
///   GET    /books     books#index
///   POST   /books     books#create
///   PUT    /books/:id books#update
///   DELETE /books/:id books#destroy
/// </summary>
public class BookListManager : MonoBehaviour
{
    [SerializeField] private GameObject bookScrollRect; // 本リストスクロールエリア
    [SerializeField] private GameObject bookItemPrefab; // 本アイテムPrefab
    
    /// <summary>
    /// 開始処理
    /// </summary>
    private void Start()
    {
        // 本情報を取得
        GetBookList();
    }
    
    /// <summary>
    /// 本アイテムを追加する
    /// CREATE
    /// </summary>
    /// <param name="bookItemContent">追加する本情報</param>
    private void AddBookItem(BookItemContent bookItemContent)
    {
        // jsonデータの作成
        var schema = new BookSchema
        {
            name = bookItemContent.nameInput.text,
            price = int.Parse(bookItemContent.priceInput.text)
        };
        var json = JsonUtility.ToJson(schema);
        
        // byte配列に変換
        var bodyRaw = Encoding.UTF8.GetBytes(json);
        
        // リクエストを送る
        var url = "http://127.0.0.1:3000/books/";
        var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        StartCoroutine(SendRequest(request, response =>
        {
            // 本情報を再取得
            ClearBookList();
            GetBookList();
        }));
    }

    /// <summary>
    /// 全ての本アイテムを取得する
    /// READ
    /// </summary>
    private void GetBookList()
    {
        // リクエストを送る
        var url = "http://127.0.0.1:3000/books";
        var request = UnityWebRequest.Get(url);
        StartCoroutine(SendRequest(request, response =>
        {
            var schema = JsonUtility.FromJson<BookSchemaArray>(response);
            foreach (var bookSchema in schema.books)
            {
                // 取得した本アイテムをリストに追加
                var obj = Instantiate(bookItemPrefab, bookScrollRect.transform);
                var bookItemContent = obj.GetComponent<BookItemContent>();
                bookItemContent.idText.text = bookSchema.id.ToString();
                bookItemContent.nameInput.text = bookSchema.name;
                bookItemContent.priceInput.text = bookSchema.price.ToString();
                bookItemContent.updateButton.onClick.AddListener(() => { PushUpdateButton(bookItemContent); });
                bookItemContent.deleteButton.onClick.AddListener(() => { PushDeleteButton(bookSchema.id); });
            }
        }));
    }

    /// <summary>
    /// 本アイテムを更新する
    /// UPDATE
    /// </summary>
    /// <param name="bookItemContent">更新する本情報</param>
    private void UpdateBookItem(BookItemContent bookItemContent)
    {
        // jsonデータの作成
        var schema = new BookSchema
        {
            name = bookItemContent.nameInput.text,
            price = int.Parse(bookItemContent.priceInput.text)
        };
        var json = JsonUtility.ToJson(schema);
        
        // byte配列に変換
        var bodyRaw = Encoding.UTF8.GetBytes(json);
        
        // リクエストを送る
        var url = "http://127.0.0.1:3000/books/" + bookItemContent.idText.text;
        var request = new UnityWebRequest(url, "PUT");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        StartCoroutine(SendRequest(request, response =>
        {
            // 本情報を再取得
            ClearBookList();
            GetBookList();
        }));
    }

    /// <summary>
    /// 本アイテムを削除する
    /// DELETE
    /// </summary>
    /// <param name="id">削除する本ID</param>
    private void DeleteBookItem(int id)
    {
        // リクエストを送る
        var url = "http://127.0.0.1:3000/books/" + id;
        var request = UnityWebRequest.Delete(url);
        StartCoroutine(SendRequest(request, response =>
        {
            // 本情報を再取得
            ClearBookList();
            GetBookList();
        }));
    }
    
    /// <summary>
    /// リクエストを送る
    /// </summary>
    /// <param name="request"></param>
    /// <param name="callback"></param>
    private IEnumerator SendRequest(UnityWebRequest request, Action<string> callback = null)
    {
        // リクエストを送る
        yield return request.SendWebRequest();
        
        // レスポンスを出力
        if (request.result == UnityWebRequest.Result.ConnectionError 
            || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log(request.error);
        }
        else
        {
            // コールバックを実行
            if (callback != null)
            {
                callback(request.downloadHandler?.text);
            }
        }
    }

    /// <summary>
    /// 本リストをクリアする
    /// </summary>
    private void ClearBookList()
    {
        foreach (Transform child in bookScrollRect.gameObject.transform)
        {
            Destroy(child.transform.gameObject);
        }
    }

    /// <summary>
    /// JSON型定義
    /// </summary>
    [Serializable]
    private class BookSchema
    {
        public int id;
        public string name;
        public int price;
    }
    [Serializable]
    private class BookSchemaArray
    {
        public BookSchema[] books;
    }

    // ---------- 各ボタン押下処理 ----------
    public void PushReloadButton()
    {
        ClearBookList();
        GetBookList();
    }
    
    public void PushAddButton(BookItemContent bookItemContent)
    {
        AddBookItem(bookItemContent);
    }

    private void PushDeleteButton(int id)
    {
        DeleteBookItem(id);
    }

    private void PushUpdateButton(BookItemContent bookItemContent)
    {
        UpdateBookItem(bookItemContent);
    }
    
    // ---------- application/x-www-form-urlencoded バージョン ----------
    
    /// <summary>
    /// 本アイテムを追加する(WWWForm使用)
    /// CREATE
    /// </summary>
    /// <param name="bookItemContent">追加する本情報</param>
    private void AddBookItemForm(BookItemContent bookItemContent)
    {
        // formデータの作成
        var form = new WWWForm();
        form.AddField("name", bookItemContent.nameInput.text);
        form.AddField("price", int.Parse(bookItemContent.priceInput.text));
        
        // リクエストを送る
        var url = "http://127.0.0.1:3000/books";
        var request = UnityWebRequest.Post(url, form);
        StartCoroutine(SendRequest(request, response =>
        {
            // 本情報を再取得
            ClearBookList();
            GetBookList();
        }));
    }

    /// <summary>
    /// 本アイテムを更新する(WWWForm使用)
    /// UPDATE
    /// </summary>
    /// <param name="bookItemContent">更新する本情報</param>
    private void UpdateBookItemForm(BookItemContent bookItemContent)
    {
        // formデータの作成
        var form = new WWWForm();
        form.AddField("name", bookItemContent.nameInput.text);
        form.AddField("price", int.Parse(bookItemContent.priceInput.text));

        // リクエストを送る
        var url = "http://127.0.0.1:3000/books/" + bookItemContent.idText.text;
        var request = UnityWebRequest.Post(url, form);
        request.method = "PUT"; // PUTを指定
        StartCoroutine(SendRequest(request, response =>
        {
            // 本情報を再取得
            ClearBookList();
            GetBookList();
        }));
    }
}
