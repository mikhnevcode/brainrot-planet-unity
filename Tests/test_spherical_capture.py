"""Geometry regression for 4-neighbor wraparound capture, independently from Unity.

Tests a discrete loop between two points of an owned circular area. This is a
mathematical reference check, not proof that Unity compiles or the APK runs.
"""
import math
import unittest

W,H=128,64

def index(x,y): return y*W+(x%W)
def weight(k): return math.sin(math.pi*((k//W)+.5)/H)

def fill_around(u,v,r=.085):
    result=set()
    for k in range(W*H):
        x,y=(k%W+.5)/W,(k//W+.5)/H
        dx=min(abs(x-u),1-abs(x-u))
        if dx*dx+(y-v)**2<r*r: result.add(k)
    return result

def draw(a,b,out,step=700):
    for i in range(step+1):
        t=i/step
        x=(a[0]+(b[0]-a[0])*t)%1
        y=a[1]+(b[1]-a[1])*t
        xx=int(x*W)%W;yy=max(0,min(H-1,int(y*H)))
        out.update([index(xx,yy),index(xx+1,yy),index(xx-1,yy),
                    index(xx,yy+1) if yy+1<H else index(xx,yy),
                    index(xx,yy-1) if yy>0 else index(xx,yy)])

def enclosed(owned, trail):
    solid=owned|trail
    visited=set();components=[]
    for first in range(W*H):
        if first in visited or first in solid:continue
        q=[first];visited.add(first);region=[]
        for k in q:
            region.append(k);x=k%W;y=k//W
            neighbors=[index(x+1,y),index(x-1,y)]
            if y>0:neighbors.append(index(x,y-1))
            if y<H-1:neighbors.append(index(x,y+1))
            for j in neighbors:
                if j not in visited and j not in solid:
                    visited.add(j);q.append(j)
        components.append(region)
    if len(components)<2:return set(trail),len(components)
    best=min(components,key=lambda region:sum(map(weight,region)))
    return owned|trail|set(best),len(components)

class SphericalCaptureTests(unittest.TestCase):
    def test_closed_route_paints_region(self):
        owned=fill_around(.5,.5,.075)
        t=set()
        vertices=[(.50,.57),(.63,.66),(.73,.54),(.64,.40),(.50,.43)]
        for a,b in zip(vertices,vertices[1:]):draw(a,b,t)
        result,n=enclosed(owned,t)
        self.assertGreaterEqual(n,2)
        self.assertGreater(len(result),len(owned)+len(t))
        self.assertLess(len(result),W*H*.35)
    def test_route_never_closes_returns_just_line(self):
        owned=fill_around(.5,.5,.075);t=set()
        draw((.5,.56),(.69,.65),t)
        result,n=enclosed(owned,t)
        self.assertEqual(n,1)
        self.assertEqual(result,t)
    def test_longitude_wraparound_connects(self):
        owned=fill_around(.98,.52,.07);t=set()
        vertices=[(.98,.58),(1.10,.67),(1.20,.54),(1.10,.39),(.98,.46)]
        for a,b in zip(vertices,vertices[1:]):draw(a,b,t)
        result,n=enclosed(owned,t)
        self.assertGreaterEqual(n,2)
        self.assertTrue(any(k%W<4 for k in result))
        self.assertTrue(any(k%W>W-4 for k in result))

if __name__=='__main__':unittest.main()
