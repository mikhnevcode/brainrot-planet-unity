#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    public static class GameplayFeedbackSmoke
    {
        [MenuItem("Brainrot Planet/Tests/Gameplay feedback smoke")]
        public static void Run()
        {
            Require(GameplayFeedback.TrailRiskFor(0,0f)==TrailRisk.None,"closed trail");
            Require(GameplayFeedback.TrailRiskFor(20,80f)==TrailRisk.Short,"short trail");
            Require(GameplayFeedback.TrailRiskFor(70,80f)==TrailRisk.Exposed,"long trail");
            Require(GameplayFeedback.TrailRiskFor(20,20f)==TrailRisk.Exposed,"near enemy");
            Require(GameplayFeedback.TrailRiskFor(150,80f)==TrailRisk.Critical,"critical length");
            Require(GameplayFeedback.TrailRiskFor(20,10f)==TrailRisk.Critical,"critical enemy");
            Require(GameplayFeedback.DirectionLabel(Vector2.zero).StartsWith("СТОП"),"stopped");
            Require(GameplayFeedback.DirectionLabel(Vector2.up)=="ВВЕРХ","up");
            Require(GameplayFeedback.DirectionLabel(Vector2.right)=="ВПРАВО","right");
            Require(GameplayFeedback.IsTap(Vector2.zero,new Vector2(3f,4f),false,.2f),"short tap");
            Require(!GameplayFeedback.IsTap(Vector2.zero,new Vector2(3f,4f),true,.2f),"drag is not tap");
            Require(!GameplayFeedback.IsTap(Vector2.zero,Vector2.zero,false,.5f),"hold is not tap");
            Rect frame=new Rect(27f,35f,366f,690f);
            Require(OnEdge(GameplayFeedback.PerimeterPoint(Vector2.right,frame),frame),"right perimeter");
            Require(OnEdge(GameplayFeedback.PerimeterPoint(new Vector2(-2f,-1f),frame),frame),"diagonal perimeter");
            Require(OnEdge(GameplayFeedback.PerimeterPoint(Vector2.zero,frame),frame),"fallback perimeter");
            Matrix4x4 scale=Matrix4x4.Scale(new Vector3(1.714f,1.714f,1f));
            Vector2 pivot=new Vector2(27f,410f);
            Matrix4x4 rotated=GameplayFeedback.RotateAroundLogicalPivot(scale,137f,pivot);
            Vector3 before=scale.MultiplyPoint3x4(pivot);
            Vector3 after=rotated.MultiplyPoint3x4(pivot);
            Require(Vector3.Distance(before,after)<.01f,"scaled rotation keeps perimeter pivot");
            Debug.Log("GAMEPLAY_FEEDBACK_SMOKE_PASS risks=6 directions=3 tap=3 perimeter=3 scaledPivot=true");
        }

        private static bool OnEdge(Vector2 point,Rect rect)
        {
            bool inside=point.x>=rect.xMin-.01f&&point.x<=rect.xMax+.01f&&
                        point.y>=rect.yMin-.01f&&point.y<=rect.yMax+.01f;
            bool edge=Mathf.Abs(point.x-rect.xMin)<.01f||Mathf.Abs(point.x-rect.xMax)<.01f||
                      Mathf.Abs(point.y-rect.yMin)<.01f||Mathf.Abs(point.y-rect.yMax)<.01f;
            return inside&&edge;
        }

        private static void Require(bool condition,string name)
        {
            if(!condition)throw new Exception("Gameplay feedback failed: "+name);
        }
    }
}
#endif
