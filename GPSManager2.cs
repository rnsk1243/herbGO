using UnityEngine;
using System.Collections;

public enum LocationState{
	Disabled,
	TimedOut,
	Failed,
	Enabled
}

public class GPSManager2 : MonoBehaviour {
	public const float ErrorRange = 0.0001f;
	public static int SCREEN_DENSITY;
	private GUIStyle debugStyle;
	// Approximate radius of the earth (in kilometers)
	const float EARTH_RADIUS = 6371;
	private LocationState state;
	// Position on earth (in degrees)
	private float latitude;
	private float longitude;
	// Distance walked (in meters)
	private float distance;
	// Coins obtained (1 for every 100 meters walked)
	//private int coins;

	//PhotonView 컴포넌트를 할당할 변수
	private PhotonView pv = null;

	private static float currLati;
	private static float currLongi;
	//public int currUserID;

	// 약초를 발견 했니?
	private static bool isItemShow;



	void Awake()
	{
		// 초기화
		pv = GetComponent<PhotonView>();
		// 데이터 전송 타입을 설정 (변경 사항이 있을때만 전송)
		pv.synchronization = ViewSynchronization.Unreliable;
		// PhotonView Observed Components 속성에 TankMove 스크립트를 연결
		pv.ObservedComponents[0] = this;
		currLati = -1.0f;
		currLongi = -1.0f;
		//currUserID = PhotonNetwork.player.ID;
		isItemShow = false;

	}

	// Use this for initialization
	IEnumerator Start () {



		if(Screen.dpi > 0f){
			SCREEN_DENSITY = (int)(Screen.dpi / 160f);
		}else{
			SCREEN_DENSITY = (int)(Screen.currentResolution.height / 600);
		}
		debugStyle = new GUIStyle ();
		debugStyle.fontSize = 16 * SCREEN_DENSITY;
		debugStyle.normal.textColor = Color.white;

		state = LocationState.Disabled;
		latitude = 0f;
		longitude = 0f;
		distance = 0f;
		//coins = 0;


		if(Input.location.isEnabledByUser){
			Input.location.Start();
			int waitTime = 15;
			while(Input.location.status == LocationServiceStatus.Initializing && waitTime > 0){
				yield return new WaitForSeconds(1);
				waitTime--;
			}
			if(waitTime == 0){
				state = LocationState.TimedOut;
			}else if(Input.location.status == LocationServiceStatus.Failed){
				state = LocationState.Failed;
			}else{
				state = LocationState.Enabled;
				latitude = Input.location.lastData.latitude;
				longitude = Input.location.lastData.longitude;
			}
		}
		// 마스터 클래스만 아이템 유무 위치 확인 코루틴을 실행 시키면 되므로
		if(PhotonNetwork.isMasterClient)
		{
			StartCoroutine(this.isItem());
		}
		
	}

	IEnumerator OnApplicationPause(bool pauseState){
		if(pauseState){
			Input.location.Stop();
			state = LocationState.Disabled;
		}else{
			Input.location.Start();
			int waitTime = 15;
			while(Input.location.status == LocationServiceStatus.Initializing && waitTime > 0){
				yield return new WaitForSeconds(1);
				waitTime--;
			}
			if(waitTime == 0){
				state = LocationState.TimedOut;
			}else if(Input.location.status == LocationServiceStatus.Failed){
				state = LocationState.Failed;
			}else{
				state = LocationState.Enabled;
				latitude = Input.location.lastData.latitude;
				longitude = Input.location.lastData.longitude;
			}
		}
	}

	void OnGUI(){
		Rect guiBoxRect = new Rect(40, 20, Screen.width-80, Screen.height-40);

		GUI.skin.box.fontSize = 32 * SCREEN_DENSITY;
		GUI.Box (guiBoxRect, "GPS Demo");

		float buttonHeight = guiBoxRect.height / 7;

		switch(state){
		case LocationState.Enabled:
			GUILayout.Label("latitude: "+latitude.ToString(),debugStyle,GUILayout.Width(Screen.width / 4));
			GUILayout.Label("longitude: "+longitude.ToString(),debugStyle,GUILayout.Width(Screen.width / 4));

			Rect distanceTextRect = new Rect(guiBoxRect.x+40, guiBoxRect.y + guiBoxRect.height/3,
				guiBoxRect.width-80, buttonHeight);

			GUI.skin.label.fontSize = 40 * SCREEN_DENSITY;
			GUI.skin.label.alignment = TextAnchor.UpperCenter;
			GUI.Label(distanceTextRect, "Distance walked: "+distance.ToString()+"m");

			Rect coinsTextRect = new Rect(guiBoxRect.x+40, guiBoxRect.y + guiBoxRect.height*2/3,
				guiBoxRect.width-80, buttonHeight);

			GUI.skin.label.fontSize = 40 * SCREEN_DENSITY;
			GUI.skin.label.alignment = TextAnchor.UpperCenter;
			GUI.Label(coinsTextRect, "약초 발견: "+isItemShow.ToString());
			break;
		case LocationState.Disabled:
			Rect disabledTextRect = new Rect(guiBoxRect.x+40, guiBoxRect.y + guiBoxRect.height/2,
				guiBoxRect.width-80, buttonHeight*2);

			GUI.skin.label.fontSize = 40 * SCREEN_DENSITY;
			GUI.skin.label.alignment = TextAnchor.UpperCenter;
			GUI.Label(disabledTextRect, "GPS is disabled. GPS must be enabled\n" +
				"in order to use this application.");
			break;
		case LocationState.Failed:
			Rect failedTextRect = new Rect(guiBoxRect.x+40, guiBoxRect.y + guiBoxRect.height/2,
				guiBoxRect.width-80, buttonHeight*2);

			GUI.skin.label.fontSize = 40 * SCREEN_DENSITY;
			GUI.skin.label.alignment = TextAnchor.UpperCenter;
			GUI.Label(failedTextRect, "Failed to initialize location service.\n" +
				"Try again later.");
			break;
		case LocationState.TimedOut:
			Rect timeOutTextRect = new Rect(guiBoxRect.x+40, guiBoxRect.y + guiBoxRect.height/2,
				guiBoxRect.width-80, buttonHeight*2);

			GUI.skin.label.fontSize = 40 * SCREEN_DENSITY;
			GUI.skin.label.alignment = TextAnchor.UpperCenter;
			GUI.Label(timeOutTextRect, "Connection timed out. Try again later.");
			break;
		}
	}

	// The Haversine formula
	// Veness, C. (2014). Calculate distance, bearing and more between
	//	Latitude/Longitude points. Movable Type Scripts. Retrieved from
	//	http://www.movable-type.co.uk/scripts/latlong.html
	float Haversine(ref float lastLatitude,ref float lastLongitude){
		float newLatitude = Input.location.lastData.latitude;
		float newLongitude = Input.location.lastData.longitude;
		float deltaLatitude = (newLatitude - lastLatitude) * Mathf.Deg2Rad;
		float deltaLongitude = (newLongitude - lastLongitude) * Mathf.Deg2Rad;
		float a = Mathf.Pow(Mathf.Sin(deltaLatitude / 2),2) +
			Mathf.Cos(lastLatitude * Mathf.Deg2Rad) * Mathf.Cos(newLatitude * Mathf.Deg2Rad) *
			Mathf.Pow(Mathf.Sin(deltaLongitude / 2),2);
		lastLatitude = newLatitude;
		lastLongitude = newLongitude;
		float c = 2 * Mathf.Atan2(Mathf.Sqrt(a),Mathf.Sqrt(1-a));
		return EARTH_RADIUS * c;
	}

	void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		//Debug.Log("나의 userID = " + currUserID + "입니다. 서버야 일하고 있니?");
		//Debug.Log("나의 userName = " + PhotonNetwork.player.name + "입니다. 서버야 일하고 있니?");
		if(stream.isWriting)
		{
//			Debug.Log("발신 PhotonNetwork.isMasterClient = " + PhotonNetwork.isMasterClient);
			if(PhotonNetwork.isMasterClient)
			{
//				Debug.Log("마스터 입장한 방 이름 = " + PhotonNetwork.room.name);
				//stream.SendNext("화이팅");
				// 현재 있는 위치에 아이템이 있는지 없는지 보낸다. true가 보내지면 아이템이 존재한다고 클라이언트에게 보내는 것이됨.
				stream.SendNext(isItemShow);
				//stream.SendNext(true);
			}
			else
			{
				// 마스터 클라이언트가 아니면
				//Debug.Log("아이폰아 데이터 보내고 있나?");
				//stream.SendNext("우희야");
				//Debug.Log("손님 입장한 방 이름 = " + PhotonNetwork.room.name);
	
				//Debug.Log("아이폰 정보를 보낸다. 경도 = " + latitude);
				// 송신
				// 자신의 위도 경도 값을 보냄
				stream.SendNext(latitude);
				stream.SendNext(longitude);
				
				//stream.SendNext(pv.viewID);
			}
		}else
		{
//			Debug.Log("수신 PhotonNetwork.isMasterClient = " + PhotonNetwork.isMasterClient);
			// 수신
			// 방장만 수신 함.
			if(PhotonNetwork.isMasterClient)
			{
//				Debug.Log("아 제발!!");
				
				currLati = Mathf.Round((float)stream.ReceiveNext() * 10000) / 10000;
				currLongi = Mathf.Round((float)stream.ReceiveNext() * 10000) / 10000;
				//currUserID = (int)stream.ReceiveNext();
//				Debug.Log("아이폰 정보를 받았다. 경도 = " + latitude);

				//Debug.Log("currLati = " + currLati);
				//Debug.Log("currLongi = " + currLongi);

				//Debug.Log("currUserID = " + currUserID);
			}
			else{
			// 마스터 클라이언트가 아니면
				//Debug.Log((bool)stream.ReceiveNext());
				// 마스터 클라이언트가 보낸 아이템 유무 결과값을 받음
				isItemShow = (bool)stream.ReceiveNext();
				//Debug.Log("마스터에게 받은 결과 값 = "+ isItemShow);
			}
		}
	}

	// 내 위치에서 아이템 유무 확인
	IEnumerator isItem()
	{
		// 너무 자주 하면 힘드니까 1초간 딜레이
		yield return new WaitForSeconds(1.0f);
		//Debug.Log("이곳에 아이템이 있는지 확인합니다.(코루틴)" + " // PhotonInit2.LatiArray[0] = " + PhotonInit2.LatiArray[0]);

		//BSTree tree = new BSTree();
		//tree.Insert(0.1f);
		// 우선 lati확인 
		foreach(float lati in PhotonInit2.LatiArray)
		{
			Debug.Log("lati = " + lati);
			if(lati == currLati)
			{
				foreach(float longi in PhotonInit2.LongiArray)
				{
					Debug.Log("longi = " + longi);
					if(longi == currLongi)
					{
						isItemShow = true;
						//Debug.Log("약초발견 = " + isItemShow.ToString());
						//PhotonInit2.LatiArray.Remove(lati);
						//PhotonInit2.LongiArray.Remove(longi);
						break;
					}
				}
				break;
			}
			else
			{
				isItemShow = false;
			}
		}

		/*
		if(myLatiTree.GetComponent<BSTree>().GetNode(currLati) == null 
		&& myLatiTree.GetComponent<BSTree>().GetNode(currLati + ErrorRange) == null 
		&& myLatiTree.GetComponent<BSTree>().GetNode(currLati - ErrorRange) == null)
		{
			
			myLatiTree.GetComponent<BSTree>().Insert(37.2911f);
				myLogiTree.GetComponent<BSTree>().Insert(127.1421f);

				myLatiTree.GetComponent<BSTree>().Insert(37.2912f);
				myLogiTree.GetComponent<BSTree>().Insert(127.1422f);

				myLatiTree.GetComponent<BSTree>().Insert(37.2913f);
				myLogiTree.GetComponent<BSTree>().Insert(127.1423f);

				myLatiTree.GetComponent<BSTree>().Insert(37.2914f);
				myLogiTree.GetComponent<BSTree>().Insert(127.1424f);

				myLatiTree.GetComponent<BSTree>().Insert(37.2915f);
				myLogiTree.GetComponent<BSTree>().Insert(127.1425f);

				myLatiTree.GetComponent<BSTree>().Insert(37.2916f);
				myLogiTree.GetComponent<BSTree>().Insert(127.1426f);
				
			Debug.Log("실패 Lati = " + currLati + " // Longi = " + currLongi + " // latitude(37.2913f) = " + myLatiTree.GetComponent<BSTree>().GetNode(37.2913f));
			// lati만 일치하지 않으면 longi는 볼것도 없음
			isItemShow = false;

		}else{
			// 여기서 true or false 를 리턴함. (true이면 아이템 존재함.)
			isItemShow = (myLogiTree.GetComponent<BSTree>().GetNode(currLongi) != null 
			|| myLogiTree.GetComponent<BSTree>().GetNode(currLongi + ErrorRange) != null 
			|| myLogiTree.GetComponent<BSTree>().GetNode(currLongi - ErrorRange) != null);
			if(isItemShow)
			{
				Debug.Log("오 발견했음 Lati = " + currLati + " // Longi = " + currLongi);
			}
			
		}
		*/
		// 아이템 유무 확인 코루틴 실행 1초마다 재귀함수
		StartCoroutine(this.isItem());
	}

	// Update is called once per frame
	void Update () {

		if(!PhotonNetwork.isMasterClient)
		{
			if(state == LocationState.Enabled){
				

				float deltaDistance = Haversine(ref latitude,ref longitude) * 1000f;
				if(deltaDistance > 0f){
					distance += deltaDistance;
					//coins = (int)(distance / 100f);
				}

				

			}
		}else{
			// 마스터 클래스가 할 일
			
				
		}
	}
}
