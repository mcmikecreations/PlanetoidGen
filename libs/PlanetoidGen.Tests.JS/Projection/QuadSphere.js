class QuadSphere
{
	static forward_distort(chi, psi)
	{
		let chi2 = chi*chi;
		let chi3 = chi*chi*chi;
		let psi2 = psi*psi;
		let omchi2 = 1.0 - chi2;
		return 	chi*(1.37484847732 - 0.37484847732*chi2) +
				chi*psi2*omchi2*(-0.13161671474 +
								 0.136486206721*chi2 +
								 (1.0 - psi2) *
								 (0.141189631152 +
								  psi2*(-0.281528535557 + 0.106959469314*psi2) +
								  chi2*(0.0809701286525 +
										0.15384112876*psi2 -
										0.178251207466*chi2))) +
				chi3*omchi2*(-0.159596235474 -
							(omchi2 * (0.0759196200467 - 0.0217762490699*chi2)));
	}
	static inverse_distort(x,y)
	{
		let x2 = x*x;
		let x4 = x2*x2;
		let x6 = x4*x2;
		let x8 = x4*x4;
		let x10 = x8*x2;
		let x12 = x8*x4;
		
		let y2 = y*y;
		let y4 = y2*y2;
		let y6 = y4*y2;
		let y8 = y4*y4;
		let y10 = y8*y2;
		let y12 = y8*y4;
		
		return x + x*(1 - x2) *
				(-0.27292696 - 0.07629969 * x2 -
				 0.22797056 * x4 + 0.54852384 * x6 -
				 0.62930065 * x8 + 0.25795794 * x10 +
				 0.02584375 * x12 - 0.02819452 * y2 -
				 0.01471565 * x2 * y2 + 0.48051509 * x4 * y2 -
				 1.74114454 * x6 * y2 + 1.71547508 * x8 * y2 -
				 0.53022337 * x10 * y2 + 0.27058160 * y4 -
				 0.56800938 * x2 * y4 + 0.30803317 * x4 * y4 +
				 0.98938102 * x6 * y4 - 0.83180469 * x8 * y4 -
				 0.60441560 * y6 + 1.50880086 * x2 * y6 -
				 0.93678576 * x4 * y6 + 0.08693841 * x6 * y6 +
				 0.93412077 * y8 - 1.41601920 * x2 * y8 +
				 0.33887446 * x4 * y8 - 0.63915306 * y10 +
				 0.52032238 * x2 * y10 + 0.14381585 * y12)
	}
	static forward(phi, theta)
	{
		let tArr4 = this.Tangential.forward(phi, theta);
		let face = tArr4[0];
		let chi = tArr4[1];
		let psi = tArr4[2];
		
		return [ face, 
				 this.forward_distort(chi, psi), 
				 this.forward_distort(psi, chi) ];
	}
	static inverse(face, x, y)
	{
		let chi = this.inverse_distort(x,y);
		let psi = this.inverse_distort(y,x);
		return this.Tangential.inverse(face, chi, psi);
	}
}
QuadSphere.TOP_FACE = 0;
QuadSphere.FRONT_FACE = 1;
QuadSphere.EAST_FACE = 2;
QuadSphere.BACK_FACE = 3;
QuadSphere.WEST_FACE = 4;
QuadSphere.BOTTOM_FACE = 5;
QuadSphere.Tangential = class
{
	//Information for each face.  Faces are given in the order:
	//top, front, left, back, right, bottom.
	//These procedures return the direction cosines:
	//1. l (cos(θ)*cos(φ))
	//2. m (cos(θ)*sin(φ))
	//3. n (sin(θ))
	static FORWARD_PARAMETERS(index)
	{
		if (index == 0) return function(l,m,n){ return [m, -l, n]; };
		else if (index == 1) return function(l,m,n){ return [m, n, l]; };
		else if (index == 2) return function(l,m,n){ return [-l, n, m]; };
		else if (index == 3) return function(l,m,n){ return [-m, n, -l]; };
		else if (index == 4) return function(l,m,n){ return [l, n, -m]; };
		else if (index == 5) return function(l,m,n){ return [m, l, -n]; };
	}
	//Computes the projection of a point on the surface of the sphere,
	//given in spherical coordinates (φ,θ), to a point of cartesian
	//coordinates (χ,ψ) on one of the six cube faces
	//phi   : [-π;π] or [0;2π]	//the φ angle in radians, this is the azimuth, or longitude (spherical, not geodetic)
	//theta : [-π/2;π/2]		//the θ angle in radians, this is the elevation, or latitude (spherical, not geodetic)
	//returns an array of three elements: the identifier of
	//the face (see constants in {QuadSphere}), the χ coordinate of
	//the projected point, and the ψ coordinate of the projected
	//point.  Both coordinates will be in the range -1 to 1
	static forward(phi, theta)
	{
		let l = Math.cos(theta)*Math.cos(phi);
		let m = Math.cos(theta)*Math.sin(phi);
		let n = Math.sin(theta);
		
		let max = null;
		let face = -1;
		let tArr1 = [n,l,m,-l,-m,-n];
		tArr1.forEach(function (v, i) {
			if (max == null || v > max)
			{
				max = v;
				face = i;
			}
		});
		
		let tArr2 = this.FORWARD_PARAMETERS(face)(l,m,n);
		let xi = tArr2[0];
		let eta = tArr2[1];
		let zeta = tArr2[2];
		
		let chi = xi / zeta;
		let psi = eta / zeta;
		
		return [face, chi, psi];
	}
	//Information for each face.  Faces are given in the order:
	//top, front, left, back, right, bottom.
	//These procedures return the direction cosines:
	//1. l (cos(θ)*cos(φ))
	//2. m (cos(θ)*sin(φ))
	//3. n (sin(θ))
	static INVERSE_PARAMETERS(index)
	{
		if (index == 0) return function(xi,eta,zeta){ return [-eta, xi, zeta]; };
		else if (index == 1) return function(xi,eta,zeta){ return [zeta, xi, eta]; };
		else if (index == 2) return function(xi,eta,zeta){ return [-xi, zeta, eta]; };
		else if (index == 3) return function(xi,eta,zeta){ return [-zeta, -xi, eta]; };
		else if (index == 4) return function(xi,eta,zeta){ return [xi, -zeta, eta]; };
		else if (index == 5) return function(xi,eta,zeta){ return [eta, xi, -zeta]; };
	}
	//Computes the projection of a point at cartesian coordinates
	//(χ,ψ) on one of the six cube faces, to a point at spherical
	//coordinates (φ,θ) on the surface of the sphere.
	//face : [0,5]		//face (Integer) the identifier of the cube face
	//chi  : [-1.0;1.0]	//chi (Float) the χ coordinate of the point within the face
	//psi  : [-1.0;1.0]	//psi (Float) the ψ coordinate of the point within the face
	//returns an array of two elements: the φ angle in radians
	//(azimuth or longitude - spherical, not geodetic), from -π to
	//π; and the θ angle in radians, from -π/2 to π/2 (elevation or
	//latitude - spherical, not geodetic)
	static inverse(face, chi, psi)
	{
		let zeta = 1.0 / Math.sqrt(1.0+chi*chi+psi*psi);
		let xi = chi*zeta;
		let eta = psi*zeta;
		
		let tArr3 = this.INVERSE_PARAMETERS(face)(xi, eta, zeta);
		let l = tArr3[0];
		let m = tArr3[1];
		let n = tArr3[2];
		
		return [ Math.atan2(m,l), Math.asin(n) ]; //φ,θ
	}
}