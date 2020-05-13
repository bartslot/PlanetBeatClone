﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class EliminationPhase : MonoBehaviourPun {
    private bool eliminate = false;

    public Image kingCrown;

    private PlanetPositioner planetPositioner;
    private PlayerPlanets[] planets;

    public EliminationBar eliminationBar; 

    [Header("ELIMINATION")]
    public float eliminationSpeed = 2;
    public float eliminationDuration = 10f;
    public float regenSpeed = 1f;

    private PlayerPlanets eliminationTarget;

    void Start() {
        planetPositioner = GameObject.FindGameObjectWithTag("PLANETS").GetComponent<PlanetPositioner>();
        eliminationBar.gameObject.SetActive(false);
        planets = planetPositioner.GetPlanets();
    }

    void Update() {
        //KING CROWN
        PlayerPlanets highest = null;
        foreach(var i in planets) if((highest == null || i.currentScore > highest.currentScore) && i.HasPlayer() && i.currentScore > 0) highest = i;
        if(highest != null) kingCrown.transform.position = Vector3.Lerp(kingCrown.transform.position, highest.transform.position + Vector3.up / 1.5f, Time.deltaTime * 14f);
        else kingCrown.transform.position = Vector3.Lerp(kingCrown.transform.position, new Vector3(0, 50, 0), Time.deltaTime * 14f);
        kingCrown.transform.rotation = Quaternion.identity;

        if(eliminate && PhotonNetwork.IsMasterClient) {
            //TIME TICKING FOR ELIMINATION
            if(eliminationTarget != null && eliminationTarget.eliminationTimer > 0) eliminationTarget.eliminationTimer -= Time.deltaTime * eliminationSpeed;

            //FIND THE LOWEST SCORE PLAYER 
            PlayerPlanets lowest = null;
            foreach(var i in planets) if((lowest == null || i.currentScore < lowest.currentScore) && i.HasPlayer()) lowest = i;
            eliminationTarget = lowest;

            if(eliminationTarget != null) {
                var progress = Util.Map(eliminationTarget.eliminationTimer, 0, eliminationDuration, 0f, 1f);
                var color = eliminationTarget.GetColor();
                
                photonView.RPC("SynchBar", RpcTarget.All, color.r, color.g, color.b, lowest.transform.position + Vector3.down / 1.5f, progress);

                if(progress <= 0 && eliminationTarget != null) {
                    EliminatePlayer(eliminationTarget);
                    photonView.RPC("UnsynchBar", RpcTarget.All);
                    eliminate = false;
                }
            }
        }
        if(planets != null) foreach(var i in planets) if(eliminationTarget != null && i.playerNumber != eliminationTarget.playerNumber) i.RechargeElimination(eliminationDuration, regenSpeed);
    }

    [PunRPC]
    public void UnsynchBar() {
        eliminationBar.gameObject.SetActive(false);
    }

    [PunRPC]
    public void SynchBar(float r, float g, float b, Vector3 pos, float progress) {
        eliminationBar.transform.position = Vector3.Lerp(eliminationBar.transform.position, pos, Time.deltaTime * 2f);
        eliminationBar.SetProgress(new Color(r, g, b), progress);   
        eliminationBar.gameObject.SetActive(true);
    }

    public void EliminatePlayer(PlayerPlanets lowest) {
        if(lowest != null) lowest.KillPlanet();
    }

    public void StartEliminate() {
        if(eliminate) return;
        planets = planetPositioner.GetPlanets();
        foreach(var i in planets) i.SetElimination(eliminationDuration);
        eliminate = true;
    }

    public bool IsEliminating() {
        return eliminate;
    }
}