using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;



public class ParticleGen : MonoBehaviour
{
    [System.Serializable]
    struct ParticleData
    {
        public Vector3 Position;
    }

    const int SIMULATION_BLOCK_SIZE = 256;


        int ObjectNum = 1000;

        public Vector3 WallCenter = Vector3.zero;

        public Vector3 WallSize = new Vector3(32.0f, 32.0f, 32.0f);

    // #region Built-in Resources
    //     public ComputeShader FishBoidsCS;
    // #endregion

    #region Private Resources
        GraphicsBuffer _particleDataBuffer;        
    #endregion

    #region Rendering Resources
        public Mesh InstanceMesh;
        public Material InstanceRenderMaterial;
    #endregion 

    #region Private Variables
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        GraphicsBuffer argsBuffer;
    #endregion 





    #region MonoBehaviour Functions
        void Start()
        {
            InitGraphicsBuffer();
            InitRenderBuffer();
        }

        // Update is called once per frame
        void Update()
        {
            // Simulation();
            RenderInstancedMesh();
        }
        void OnDestroy()
        {
            ReleaseBuffer();
        }

        void OnDisable()
        {
            if (argsBuffer != null)
                argsBuffer.Release();
            argsBuffer = null;
        }
        void OnDrawGizmos()
        {
            // Gizmos.color = Color.cyan;
            // Gizmos.DrawWireCube(WallCenter, WallSize);
        }
    #endregion


    #region Private Functions


        void InitGraphicsBuffer()
        {
            var type = GraphicsBuffer.Target.Index | GraphicsBuffer.Target.Structured;
            _particleDataBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured, ObjectNum, Marshal.SizeOf(typeof(ParticleData))
            );
            var forceArr = new Vector3[ObjectNum];
            var particleDataArr = new ParticleData[ObjectNum];
            for (var i = 0; i < ObjectNum; i++)
            {
                particleDataArr[i].Position = Random.insideUnitSphere * 10.00f;
            }
            _particleDataBuffer.SetData(particleDataArr);

            particleDataArr = null;
        }

        void InitRenderBuffer(){
            argsBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.IndirectArguments,
                1, args.Length * sizeof(uint));
        }


        void RenderInstancedMesh(){

            if (InstanceRenderMaterial == null ||
                !SystemInfo.supportsInstancing){
                    Debug.Log("Failed");
                return;
            }


            uint numIndices = (InstanceMesh != null) ?
                (uint)InstanceMesh.GetIndexCount(0) : 0;
            args[0] = (uint)numIndices; 
            args[1] = (uint)ObjectNum; 

            argsBuffer.SetData(args); 

            InstanceRenderMaterial.SetBuffer("_particleDataBuffer",_particleDataBuffer);

            

            var bounds = new Bounds
            (
                WallCenter,
                WallSize
            );

            Graphics.DrawMeshInstancedIndirect
            (
                InstanceMesh,          
                0,                      
                InstanceRenderMaterial, 
                bounds,                 
                argsBuffer             
            );
            
        }

        
        void ReleaseBuffer()
        {
            if (_particleDataBuffer != null)
            {
                _particleDataBuffer.Release();
                _particleDataBuffer = null;
            }


        }
    #endregion
}

