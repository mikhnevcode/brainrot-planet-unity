"""Fit a locked review camera/pose to manually identified concept landmarks.

This is a diagnostic fit, not a claim of measured visual likeness. No pixels of
the reference are modified; it is not used as a character texture.
"""
import json,math
from pathlib import Path
import numpy as np
from scipy.optimize import least_squares

OUT=Path(__file__).resolve().parent/'ArtReview/RolloReferenceMatch'
REFERENCE={'hubNear':[379,884],'hubFar':[1063,877],'eyeNear':[727,129],
           'eyeFar':[897,167],'nose':[838,193],'crown':[760,31],'bellyBottom':[782,768]}


def smooth(t):
    t=max(0,min(1,t));return t*t*(3-2*t)


def rotate(axis,angle):
    a=math.radians(angle);c,s=math.cos(a),math.sin(a)
    return {'X':np.array([[1,0,0],[0,c,-s],[0,s,c]]),
            'Y':np.array([[c,0,s],[0,1,0],[-s,0,c]]),
            'Z':np.array([[c,-s,0],[s,c,0],[0,0,1]])}[axis]


def posed(point,pose):
    p=np.array(point,dtype=float);head=smooth((p[2]-1.85)/.65)
    face=smooth((p[2]-2)/.2)*smooth((p[1]-.9)/.3)*(1-smooth((abs(p[0])-.5)/.3))
    head=max(head,face);pivot=np.array([0,.3,2.3])
    moved=pivot+rotate('Z',pose[1])@rotate('Y',pose[0])@(p-pivot)
    p=p+(moved-p)*head;pivot=np.array([0,0,.9])
    return pivot+rotate('X',-pose[2])@(p-pivot)


def project(point,p):
    yaw,pitch=map(math.radians,p[:2]);scale,sx,sy=p[2:5]
    right=np.array([-math.cos(yaw),math.sin(yaw),0])
    up=np.array([-math.sin(pitch)*math.sin(yaw),-math.sin(pitch)*math.cos(yaw),math.cos(pitch)])
    q=posed(point,p[5:])-np.array([0,.15,1.7])
    return np.array([.5+np.dot(q,right)/scale+sx,.5-np.dot(q,up)/scale+sy])*1254


def main():
    source=json.loads((OUT/'source-landmarks.json').read_text())
    def residual(p):
        values=[]
        for key,target in REFERENCE.items():
            weight=1.5 if key.startswith('hub') else 1
            delta=(project(source[key],p)-target)*weight
            if key=='crown':delta[0]*=.25
            values.extend(delta)
        # Mild priors prevent a single AI-painted feature from forcing an
        # implausible photographic camera or an extreme anatomical twist.
        values.extend([(p[0]-28)*2,(p[1]-4)*1.0,(p[5]+8)*4,(p[6]-2)*2,p[7]*5])
        return values
    fit=least_squares(residual,[28,4,4.35,.02,-.07,-8,2,0],
                      bounds=([20,0,4.30,-.20,-.20,-10,-3,0],[35,10,4.8,.20,.10,-6,5,2]),max_nfev=400)
    p=fit.x
    camera=dict(zip(('yaw','pitch','scale','shiftX','shiftY'),map(float,p[:5])))
    pose=dict(zip(('headRoll','headYaw','leanForward'),map(float,p[5:])))
    for index,changes,active in [('000',['Matched camera, scale and framing only'],{'headRoll':0,'headYaw':0,'leanForward':0}),
                                 ('001',['Same locked camera; upper-body/head pose only; no material, eye or muzzle redesign'],pose)]:
        config={'camera':camera,'pose':active,'changed':changes,'sourceLandmarks':source,'referenceLandmarks':REFERENCE,
                'landmarkMethod':'Approximate manual pixel landmarks, 1254px concept; diagnostic only; no likeness score.'}
        (OUT/('iteration-'+index+'.json')).write_text(json.dumps(config,indent=2)+'\n')
    print('REFERENCE_CAMERA',camera);print('REFERENCE_POSE',pose)
    print('Diagnostic per-landmark distances, not a likeness score:')
    for key,target in REFERENCE.items():print(key,round(float(np.linalg.norm(project(source[key],p)-target)),1),'px')


if __name__=='__main__':main()
