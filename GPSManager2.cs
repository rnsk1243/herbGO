using UnityEngine;
using System.Collections;

public enum LocationState{
	Disabled,
	TimedOut,
	Failed,
	Enabled
}

public class GPSManager2 : MonoBehaviour {

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
	private int coins;

	//PhotonView 컴포넌트를 할당할 변수
	private PhotonView pv = null;

	private float currLati;
	private float currLongi;
	//public int currUserID;
	void Awake()
	{
		// 초기화
		pv = GetComponent<PhotonView>();
		// 데이터 전송 타입을 설정 (변경 사항이 있을때만 전송)
		pv.synchronization = ViewSynchronization.Unreliable;
		// PhotonView Observed Components 속성에 TankMove 스크립트를 연결
		pv.ObservedComponents[0] = this;
		currLati = 0.0f;
		currLongi = 0.0f;
		//currUserID = PhotonNetwork.player.ID;
		
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
		coins = 0;


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
			GUI.Label(coinsTextRect, "Coins: "+coins.ToString()+"g");
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
				stream.SendNext("화이팅");
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
				currLati = (float)stream.ReceiveNext();
				currLongi = (float)stream.ReceiveNext();
				//currUserID = (int)stream.ReceiveNext();
//				Debug.Log("아이폰 정보를 받았다. 경도 = " + latitude);

				Debug.Log("currLati = " + currLati);
				Debug.Log("currLongi = " + currLongi);
				
				//Debug.Log("currUserID = " + currUserID);
			}
			else{
			// 마스터 클라이언트가 아니면
				Debug.Log((string)stream.ReceiveNext());
			}
		}
	}

	// Update is called once per frame
	void Update () {
		if(!PhotonNetwork.isMasterClient)
		{
			if(state == LocationState.Enabled){
				float deltaDistance = Haversine(ref latitude,ref longitude) * 1000f;
				if(deltaDistance > 0f){
					distance += deltaDistance;
					coins = (int)(distance / 100f);
				}
			}
		}
	}
}
