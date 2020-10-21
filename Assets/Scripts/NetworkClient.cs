using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using NetworkObjects;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

public class NetworkClient : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public string serverIP;
    public ushort serverPort;
    public GameObject prefab;
    public Dictionary<string, GameObject> playerList = new Dictionary<string, GameObject>();
   // public List<string> playerIDs;
    public string newID;

    
    void Start ()
    {
        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);
        var endpoint = NetworkEndPoint.Parse(serverIP,serverPort);
        m_Connection = m_Driver.Connect(endpoint);
    }
    
    void SendToServer(string message){
        var writer = m_Driver.BeginSend(m_Connection);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message),Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }

    void OnConnect(){
        Debug.Log("We are now connected to the server");
        StartCoroutine(SendRepeatUpdate());
     
    }

    IEnumerator SendRepeatUpdate()
    {
        while(true)
        {
            
            yield return new WaitForSeconds(0.5f);
            if(!playerList.ContainsKey(newID)||playerList[newID] == null)
            {
                yield return new WaitForSeconds(0.5f);
            }
            PlayerUpdateMsg m = new PlayerUpdateMsg();
            m.player.id = newID;
            m.player.cube = playerList[newID];
            m.player.cubPos = playerList[newID].transform.position;
            Renderer renderer = m.player.cube.GetComponent<Renderer>();
            renderer.material.color = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), 1);
            m.player.cubeColor = renderer.material.color;
            SendToServer(JsonUtility.ToJson(m));
        }
    }
    void OnData(DataStreamReader stream){
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length,Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        switch(header.cmd){
            case Commands.HANDSHAKE:
            HandshakeMsg hsMsg = JsonUtility.FromJson<HandshakeMsg>(recMsg);
            newID = hsMsg.player.id;
            playerList.Add(newID, Instantiate(prefab));    
            Debug.Log("Handshake message received!");
            break;
            case Commands.PLAYER_UPDATE:
            PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
          //  Debug.Log("Player update message received!");
            break;
            case Commands.SERVER_UPDATE:
            ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
                 UpdateCube(suMsg);
                Debug.Log("Server update message received!");
            break;
            default:
            Debug.Log("Unrecognized message received!");
            break;
        }
    }

     void UpdateCube(ServerUpdateMsg suMsg)
    {
       // Debug.Log("Number of clients" + suMsg.players.Count);
       for(int i = 0; i < suMsg.players.Count; i++)
        {
            Debug.Log("clientIDs " + suMsg.players[i].id + " position " + suMsg.players[i].cubPos + " color " + suMsg.players[i].cubeColor);
            if (!playerList.ContainsKey(suMsg.players[i].id))
            {
                playerList.Add(suMsg.players[i].id, Instantiate(prefab));
                Debug.Log("adding new player");
            }
            if(suMsg.players[i].id != newID)
            {
                playerList[suMsg.players[i].id].transform.position = suMsg.players[i].cubPos;
                playerList[suMsg.players[i].id].GetComponent<Renderer>().material.color = suMsg.players[i].cubeColor;
                
            }
        }
       
    }

    void Disconnect(){
        m_Connection.Disconnect(m_Driver);
        m_Connection = default(NetworkConnection);
    }

    void OnDisconnect(){
        Debug.Log("Client got disconnected from server");
        m_Connection = default(NetworkConnection);
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
    }   
    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();
        if (Input.GetKey("w"))
        {
            //use jsons to translate your position to serverside, which will then translate the player in a direction
            //translate****************-*
            playerList[newID].transform.Translate(new Vector3(0, 1 * Time.deltaTime, 0));
        }
        if (Input.GetKey("s"))
        {
            //use jsons to translate your position to serverside, which will then translate the player in a direction
            //translate
            playerList[newID].transform.Translate(new Vector3(0, -1 * Time.deltaTime, 0));
        }
        if (Input.GetKey("a"))
        {
            //use jsons to translate your position to serverside, which will then translate the player in a direction
            //translate
            playerList[newID].transform.Translate(new Vector3(-1 * Time.deltaTime, 0, 0));
        }
        if (Input.GetKey("d"))
        {
            //use jsons to translate your position to serverside, which will then translate the player in a direction
            //translate
            playerList[newID].transform.Translate(new Vector3(1 * Time.deltaTime, 0, 0));
        }
        if (!m_Connection.IsCreated)
        {
            return;
        }

       
        DataStreamReader stream;
        NetworkEvent.Type cmd;
        cmd = m_Connection.PopEvent(m_Driver, out stream);
        while (cmd != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                OnConnect();
                
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                OnData(stream);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                OnDisconnect();
            }
            cmd = m_Connection.PopEvent(m_Driver, out stream);
        }
    }
}