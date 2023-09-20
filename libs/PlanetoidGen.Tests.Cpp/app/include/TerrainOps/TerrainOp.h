#ifndef TERRAIN_OP_H
#define TERRAIN_OP_H

#include "point.h"
#include "pointset.h"

class TerrainOp
{
public:
	TerrainOp ();

	virtual void Execute (Point& p) = 0;
	virtual void setOverride (bool b);
	virtual bool mayOverride();
	inline void setDelta (int d)								{delta = d; }
	inline int getDelta ()										{return delta;}
private:
	bool overrideFixed;
	int delta;
};

class AltitudeModifier : public TerrainOp
{
protected:
	int range_min;
	int range_max;


public:
	AltitudeModifier (int rmin, int rmax = 0) : TerrainOp()		{range_min = rmin; range_max = rmax;}
	AltitudeModifier ()	: TerrainOp()							{range_min = 0; range_max = 0;}

	inline void setRange(int rmin, int rmax = 0)				{range_min = rmin; range_max = rmax;}	
	virtual void Execute (Point& p) = 0;
};

class TextureOp : public TerrainOp
{
private:
	int texture_id;

public:
	TextureOp (int id): TerrainOp()				{ texture_id = id; }
	virtual void Execute (Point& p);
};

class SetInsertOp : public TerrainOp
{
private:
	PointSet* points;

public:
	SetInsertOp (PointSet* p) : points(p), TerrainOp()		 {}
	virtual void Execute (Point& p);
};

class SmootherOp : public AltitudeModifier
{
public:
	SmootherOp () : AltitudeModifier()		{}
	virtual void Execute (Point& p);
};

class SetHeightOp : public AltitudeModifier
{
public:
	SetHeightOp (int min_a, int max_a = 0)	: AltitudeModifier(min_a, max_a)	{}
	virtual void Execute (Point& p);
};

class FixPointOp : public TerrainOp
{
public:
	FixPointOp () : TerrainOp()			{}
	virtual void Execute (Point& p);
};

class RoughenerOp : public TerrainOp
{
public:
	RoughenerOp () : TerrainOp()			{probability = 50; variance = 10;}
	virtual void Execute (Point& p);
	void setProb (int p)					{probability = p; }
	void setVariance (int v)				{variance = v; }
private:
	int probability;
	int variance;
};

class CutChannelOp : public AltitudeModifier
{
public:
	CutChannelOp (int alt) : AltitudeModifier (alt, 0)		{}
	virtual void Execute (Point& p);
};


#endif