using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


namespace FishBoidsOnGPU
{
    public class FishBoidsOnGPU : MonoBehaviour
    {
        [System.Serializable]
        struct FishBoidData
        {
            public Vector3 Velocity;
            public Vector3 Position;
            public float AnimationOffset;
            public Vector3 Color;
            public float PiledTime;
            public float UpdateTime;
            public float Scale;
            public int Type;
            public Vector3 NextPos;
            public Vector3 forward;
            public Vector2 speeds;
        }

        const int SIMULATION_BLOCK_SIZE = 256;

        #region Boids Parameters
            [Range(0, 5000)]
            public int LargeObjectNum = 1;

            [Range(0, 5000)]
            public int MidiumObjectNum = 20;

            [Range(0, 5000)]
            public int SmallObjectNum = 4000;

            int ObjectNum;

            float[] Scales = new float[3]{0.5f, 0.03f, 0.005f};
            float[,] Speeds = new float[3,2];

            public float CohesionNeighborhoodRadius = 2.0f;
            public float AlignmentNeighborhoodRadius = 2.0f;
            public float SeparateNeighborhoodRadius = 1.0f;

            public float MaxSpeed = 5.0f;
            public float MaxSteerForce = 0.5f;

            public float CohesionWeight = 1.0f;
            public float AlignmentWeight = 1.0f;
            public float SeparateWeight = 3.0f;

            public float AvoidWallWeight = 10.0f;

            public Vector3 WallCenter = Vector3.zero;

            public Vector3 WallSize = new Vector3(32.0f, 32.0f, 32.0f);
        #endregion

        #region Built-in Resources
            public ComputeShader FishBoidsCS;
        #endregion

        #region Private Resources
            GraphicsBuffer _boidDataBuffer;        
            GraphicsBuffer _boidForceBuffer;        
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
                ObjectNum = LargeObjectNum + MidiumObjectNum + SmallObjectNum;
                Speeds[0,0] = 0.5f; Speeds[0,1] = 1.0f;
                Speeds[1,0] = 0.5f; Speeds[1,1] = 1.5f;
                Speeds[2,0] = 0.5f; Speeds[2,1] = 1.0f;
                InitGraphicsBuffer();
                InitRenderBuffer();
            }

            // Update is called once per frame
            void Update()
            {
                Simulation();
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
                // デバッグとしてシミュレーション領域をワイヤーフレームで描画
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(WallCenter, WallSize);
            }
        #endregion


        #region Private Functions
            int ItrToType(int itr){
                if(itr < LargeObjectNum) return 0;
                else if(itr < LargeObjectNum + MidiumObjectNum) return 1;
                else return 2;
            }

            void InitGraphicsBuffer()
            {
                var type = GraphicsBuffer.Target.Index | GraphicsBuffer.Target.Structured;
                _boidDataBuffer = new GraphicsBuffer(
                    GraphicsBuffer.Target.Structured, ObjectNum, Marshal.SizeOf(typeof(FishBoidData))
                );
                _boidForceBuffer = new GraphicsBuffer(
                    GraphicsBuffer.Target.Structured, ObjectNum, Marshal.SizeOf(typeof(Vector3))
                );

                var forceArr = new Vector3[ObjectNum];
                var boidDataArr = new FishBoidData[ObjectNum];
                for (var i = 0; i < ObjectNum; i++)
                {
                    forceArr[i] = Vector3.zero;
                    boidDataArr[i].Position = Random.insideUnitSphere * 1.0f;
                    boidDataArr[i].Velocity = Random.insideUnitSphere * 0.1f;
                    boidDataArr[i].forward = boidDataArr[i].Velocity * 10;
                    boidDataArr[i].AnimationOffset = 0.0f;
                    boidDataArr[i].speeds = new Vector2(Speeds[ItrToType(i), 0], Speeds[ItrToType(i), 1]);
                    boidDataArr[i].Color = Random.insideUnitSphere * 1.0f;
                    boidDataArr[i].PiledTime = 0;
                    boidDataArr[i].UpdateTime = Random.Range(1.2f, 2.0f);
                    boidDataArr[i].NextPos = boidDataArr[i].UpdateTime * boidDataArr[i].Velocity;
                    boidDataArr[i].Scale = Scales[ItrToType(i)];
                    boidDataArr[i].Type = ItrToType(i);
                }
                _boidDataBuffer.SetData(boidDataArr);
                _boidForceBuffer.SetData(forceArr);
                
                forceArr = null;
                boidDataArr = null;
            }

            void InitRenderBuffer(){
                argsBuffer = new GraphicsBuffer(
                    GraphicsBuffer.Target.IndirectArguments,
                    1, args.Length * sizeof(uint));
            }


            void Simulation()
            {
                ComputeShader cs = FishBoidsCS;
                int id = -1;

                int threadGroupSize = Mathf.CeilToInt((float)ObjectNum / (float)SIMULATION_BLOCK_SIZE);

            
                id = cs.FindKernel("ForceCS"); 
                cs.SetInt("_MaxBoidObjectNum", ObjectNum);
                cs.SetFloat("_CohesionNeighborhoodRadius", CohesionNeighborhoodRadius);
                cs.SetFloat("_AlignmentNeighborhoodRadius", AlignmentNeighborhoodRadius);
                cs.SetFloat("_SeparateNeighborhoodRadius", SeparateNeighborhoodRadius);
                cs.SetFloat("_MaxSpeed", MaxSpeed);
                cs.SetFloat("_MaxSteerForce", MaxSteerForce);
                cs.SetFloat("_SeparateWeight", SeparateWeight);
                cs.SetFloat("_CohesionWeight", CohesionWeight);
                cs.SetFloat("_AlignmentWeight", AlignmentWeight);
                cs.SetVector("_WallCenter", WallCenter);
                cs.SetVector("_WallSize", WallSize);
                cs.SetFloat("_AvoidWallWeight", AvoidWallWeight);
                cs.SetBuffer(id, "_BoidDataBufferRead", _boidDataBuffer);
                cs.SetBuffer(id, "_BoidForceBufferWrite", _boidForceBuffer);
                cs.Dispatch(id, threadGroupSize, 1, 1); 
                
                id = cs.FindKernel("UpdateNextPos");
                cs.SetFloat("_DeltaTime", Time.deltaTime);
                cs.SetBuffer(id, "_BoidForceBufferRead", _boidForceBuffer);
                cs.SetBuffer(id, "_BoidDataBufferWrite", _boidDataBuffer);
                cs.Dispatch(id, threadGroupSize, 1, 1); 

                id = cs.FindKernel("IntegrateCS");
                cs.SetFloat("_DeltaTime", Time.deltaTime);
                cs.SetBuffer(id, "_BoidForceBufferRead", _boidForceBuffer);
                cs.SetBuffer(id, "_BoidDataBufferWrite", _boidDataBuffer);
                cs.Dispatch(id, threadGroupSize, 1, 1); 
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

                // InstanceRenderMaterial.SetFloat("_DTime", Time.Time);
                InstanceRenderMaterial.SetBuffer("_BoidDataBuffer",_boidDataBuffer);

                

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
                if (_boidDataBuffer != null)
                {
                    _boidDataBuffer.Release();
                    _boidDataBuffer = null;
                }

                if (_boidForceBuffer != null)
                {
                    _boidForceBuffer.Release();
                    _boidForceBuffer = null;
                }
            }
        #endregion
    }
}
