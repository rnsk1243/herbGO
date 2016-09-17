using UnityEngine;
using System.Collections;



public class PhotonInit2 : MonoBehaviour {
	//App의 버전 정보
	public string version = "v1.0";

	public const float maxLatitude = 37.2918f;
	public const float minLatitude = 37.2908f;
	public const float maxLongitude = 127.1442f;
	public const float minLongitude = 127.1396f;

	//private PhotonView m_pv;

	public struct ItemPos
	{
		// N 위도
		private float[] latitude;
		// E 경도
		private float[] longitude;

		// 생성자
		public ItemPos(int maxSize)
		{
			latitude = new float[maxSize];
			longitude = new float[maxSize];
		}
		public void initPos()
		{
			for(int i=0; i< latitude.Length; i++)
			{
				latitude[i] = Random.Range(minLatitude, maxLatitude);
				longitude[i] = Random.Range(minLongitude, maxLongitude);
			}
		}

		public float[] getLatitude()
		{
			return latitude;
		}
		public float[] getLongitude()
		{
			return longitude;
		}

	}

	public static ItemPos itemPos;

	void Awake()
	{
		//m_pv = GameObject.Find("GPSMgr").GetComponent<PhotonView>();

		
		//포톤 클라우드에 접속
		PhotonNetwork.ConnectUsingSettings(version);
	}

	//포톤 클라우드에 정상적으로 접속한 후 로비에 입장하면 호출되는 콜백 함수
	void OnJoinedLobby()
	{
		Debug.Log("Entered Lobby!");
		// 일단 룸에 입장해 본다.
		PhotonNetwork.JoinRoom("MyRoom");
		/*
		// 로비에 입장하고 나서 룸이 만약 없으면
		if(PhotonNetwork.room == null)
		{
			
		}else{
			Debug.Log("myRoom 에 입장 합니다.");
			// room이 있으면
			PhotonNetwork.JoinRoom("MyRoom");
		}
		*/
	}

	// 룸 입장에 실패할 경우 호출되는 콜백 함수
	void OnPhotonJoinRoomFailed()
	{
		Debug.Log("myRoom 을 만듭니다.");
		//RoomOptions roomOption = new RoomOptions();
		// 룸에서 플레이어들의 UserID를 알 수 있도록 하는 옵션
		//roomOption.publishUserId = true;
		// 룸에 정상적으로 접속했으면 룸을 만든다.
		// 룸을 생성한 플레이어는 자동으로 룸에 입장한다.
		//PhotonNetwork.CreateRoom("MyRoom", roomOption, null);
		PhotonNetwork.CreateRoom("MyRoom");
	}

	// 룸에 입장하면 호출되는 콜백 함수
	void OnJoinedRoom()
	{				
		Debug.Log("Enter Room");
		// GPS매니저 생성
		CreateGPSMgr();
		//Debug.Log("나의 currUserID = " + m_photonInit.currUserID);
		// 방장인 경우
		if(PhotonNetwork.isMasterClient)
		{
//			Debug.Log("마스터 입장한 방 이름 = " + PhotonNetwork.room.name);
//			Debug.Log("마스터 클라이언트 초기화");
			// 아이템 위치 메모리 할당
			itemPos  = new ItemPos(10);
			// 아이템 위치 초기화
			itemPos.initPos();

			for(int i=0; i<10; i++)
			{
				Debug.Log("Latitude = " + itemPos.getLatitude()[i]);
				Debug.Log("Longitude = " + itemPos.getLongitude()[i]);
			}
		}
	}

	// GPSMgr를 생성하는 함수
	void CreateGPSMgr()
	{
		PhotonNetwork.Instantiate("GPSMgr", new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity, 0);
	}

	void OnGUI()
	{
		//화면 좌측 상단에 접속 과 정 에 대 한 로 그 를 출 력
		GUILayout.Label(PhotonNetwork.connectionStateDetailed.ToString());
	}

}
