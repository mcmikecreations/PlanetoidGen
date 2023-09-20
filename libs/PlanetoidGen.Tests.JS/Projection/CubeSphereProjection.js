'use strict';

/* global THREE */

function intersectPlane(/*const Vec3f &*/n, /*const Vec3f &*/p0, /*const Vec3f &*/l0, /*const Vec3f &*/l)
{
// assuming vectors are all normalized
let denom = n.dot(l);
if (denom > 1e-6) {
let p0l0 = new THREE.Vector3().subVectors(p0, l0);
let t = p0l0.dot(n) / denom;
return t;
}

return -1.0;
} 

function SetupSphere(myscene, myface, mycolor)
{
	let geometryRaw = new THREE.Geometry();
	
	const FACE_SUBDIV = 20;
	const dx = 2.0/FACE_SUBDIV;
	const dy = 2.0/FACE_SUBDIV;
	
	const n = FACE_SUBDIV+1;
	let tArr1 = new Array();
	for (let j = 0; j < n; j=j+1)
	{
		let y = -1.0 + j*dy;
		for(let i = 0; i < n; i=i+1)
		{
			let x = -1.0 + i*dx;
			let tArr2 = QuadSphere.inverse(myface,x,y);
			let lon = tArr2[0];
			let lat = tArr2[1];
			
			let tArr3 = [
				Math.cos(lat)*Math.cos(lon),
				Math.cos(lat)*Math.sin(lon),
				Math.sin(lat)
			];
			tArr1.push(tArr3);
		}
	}
	
	const row = FACE_SUBDIV+1;
	let tArr4 = new Array();
	for (let j = 0; j < FACE_SUBDIV; j=j+1)
	{
		let rowi = j*row;
		for (let i = 0; i < row; i=i+1)
		{
			let tPoint1 = tArr1[rowi+i];
			let tPoint2 = tArr1[rowi+row+i];
			tArr4.push(new THREE.Vector3(
				tPoint1[0], tPoint1[1], tPoint1[2]
			));
			tArr4.push(new THREE.Vector3(
				tPoint2[0], tPoint2[1], tPoint2[2]
			));
		}
	}
	
	let cnt = tArr4.length;
	for (let j=0; j<cnt/2-1; j=j+1)
	{
		let A = tArr4[2*j+0];
		let B = tArr4[2*j+1];
		let C = tArr4[2*j+2];
		let D = tArr4[2*j+3];
		
		let d1 = new THREE.Vector3().subVectors(A,C).length();
		let d2 = new THREE.Vector3().subVectors(B,D).length();
		if (d1 < d2)
		{
			geometryRaw.vertices.push(A);
			geometryRaw.vertices.push(B);
			geometryRaw.vertices.push(C);
			
			geometryRaw.vertices.push(A);
			geometryRaw.vertices.push(C);
			geometryRaw.vertices.push(D);
		}
		else
		{
			geometryRaw.vertices.push(A);
			geometryRaw.vertices.push(B);
			geometryRaw.vertices.push(D);
			
			geometryRaw.vertices.push(D);
			geometryRaw.vertices.push(B);
			geometryRaw.vertices.push(C);
		}
	}
	
	const material = new THREE.LineBasicMaterial( { color: mycolor, linewidth: 4 } );
	const wireframe = new THREE.Line( geometryRaw, material );
	myscene.add(wireframe);
}

function SetupPlane(myscene, myface, mycolor)
{
	let geometryRaw = new THREE.Geometry();
	
	const size = 100;
	const delta = Math.PI/(2*size*Math.sqrt(2.0));
	const meridian = 36;
	const parallel = 18;
	
	let drawLine = function(lon,lat)
	{
		let tArr1 = QuadSphere.forward(lon,lat);
		let face = tArr1[0];
		let x = tArr1[1];
		let y = tArr1[2];
		if (face == 0)
		{
			//x += 4;
			geometryRaw.vertices.push(new THREE.Vector3(x,y,1.0));
		}
		else if (face == 1)
		{
			//y += 2;
			geometryRaw.vertices.push(new THREE.Vector3(x,1.0,y));
		}
		else if (face == 2)
		{
			//x += 2;
			//y += 2;
			geometryRaw.vertices.push(new THREE.Vector3(1.0,x,y));
		}
		else if (face == 3)
		{
			//x += 4;
			//y += 2;
			geometryRaw.vertices.push(new THREE.Vector3(-1.0,x,y));
		}
		else if (face == 4)
		{
			//x += 6;
			//y += 2;
			geometryRaw.vertices.push(new THREE.Vector3(x,-1.0,y));
		}
		else if (face == 5)
		{
			//x += 4;
			//y += 4;
			geometryRaw.vertices.push(new THREE.Vector3(x,y,-1.0));
		}
	};
	
	for(let i=0;i<meridian;i=i+1)
	{
		let lon = -Math.PI+i*Math.PI*2/meridian;
		for(let j=-Math.PI/2;j<=Math.PI/2;j=j+delta)
		{
			drawLine(lon,j);
		}
	}
	
	for(let j=0;j<parallel;j=j+1)
	{
		let lat = -Math.PI/2.0+j*Math.PI/parallel;
		for(let i=-Math.PI;i<=Math.PI;i=i+delta)
		{
			drawLine(i,lat);
		}
	}
	
	const material = new THREE.PointsMaterial( { color: 0xffffff, size: 2.5, sizeAttenuation: false } );
	const wireframe = new THREE.Points( geometryRaw, material );
	myscene.add(wireframe);
}

function SetupPoints(scene)
{
	let sphereGeometry = new THREE.Geometry();
	let planeGeometry = new THREE.Geometry();
	
	//let FACE_SUBDIV = 20;
	//let dx = 2.0/FACE_SUBDIV;
	//let dy = 2.0/FACE_SUBDIV;
	//for (let j = 0; j < FACE_SUBDIV; j=j+1)
	//{
	//	let y = -1.0 + j*dy;
	//	for(let i = 0; i < FACE_SUBDIV; i=i+1)
	//	{
	//		let x = -1.0 + i*dx;
	//		let tArr = QuadSphere.inverse(QuadSphere.TOP_FACE,x,y);
	//		let lon = tArr[0];
	//		let lat = tArr[1];
	//		
	//		planeGeometry.vertices.push(new THREE.Vector3(
	//			Math.cos(lat)*Math.cos(lon),
	//			Math.cos(lat)*Math.sin(lon),
	//			Math.sin(lat)));
	//	}
	//}
	
	let dotSphereMaterial = new THREE.PointsMaterial( { color: 0xffffff, size: 2.5, sizeAttenuation: false } );
	let dotPlaneMaterial = new THREE.PointsMaterial( { color: 0xff9999, size: 2.5, sizeAttenuation: false } );
	let dotSphere = new THREE.Points( sphereGeometry, dotSphereMaterial );
	let dotPlane = new THREE.Points( planeGeometry, dotPlaneMaterial );

	const dots = new THREE.Mesh();
	dots.add( dotSphere );
	dots.add( dotPlane );
	scene.add(dots);
}

function main() {
	const canvas = document.querySelector('#c');
	const renderer = new THREE.WebGLRenderer({canvas});

	const fov = 75;
	const aspect = 4.0/3.0;  // the canvas default
	const near = 0.1;
	const far = 20;
	const camera = new THREE.PerspectiveCamera(fov, aspect, near, far);
	camera.position.set(2, 2, 3);

	const controls = new THREE.OrbitControls(camera, canvas);
	controls.target.set(0, 0, 0);
	controls.update();

	const scene = new THREE.Scene();

	{
		const radius = 1;
		const widthSegments = 12;
		const heightSegments = 8;

		const geometry = (new THREE.SphereBufferGeometry(radius, widthSegments, heightSegments));
		const material = new THREE.MeshBasicMaterial( { color: 0xffffff, linewidth: 4 } );
		const wireframe = new THREE.Mesh( geometry, material );
		scene.add(wireframe);
	}
	{
		const width = 2;
		const height = 2;
		const widthSegments = 1;
		const heightSegments = 1;
		
		const geometry = new THREE.EdgesGeometry(new THREE.PlaneBufferGeometry(width, height, widthSegments, heightSegments));
		const material = new THREE.LineBasicMaterial( { color: 0x99ff99, linewidth: 4 } );
		const wireframe = new THREE.LineSegments( geometry, material );
		wireframe.position.z = 1.0;
		scene.add(wireframe);

		//const geometry = new MeshLine(new THREE.PlaneBufferGeometry(width, height, widthSegments, heightSegments));
		//const material = new MeshLineMaterial( { color: 0xffffff, lineWidth: 4 } );
		//const wireframe = new THREE.Mesh(geometry.geometry, material);
		//scene.add(wireframe);
	}
	
	SetupPoints(scene);
	
	function RGBToHex(r, g, b) {
		return "#" + ((1 << 24) + (r << 16) + (g << 8) + b).toString(16).slice(1);
	}
	function HSLToRGB(h,s,l) {
		// Must be fractions of 1
		s /= 100;
		l /= 100;

		let c = (1 - Math.abs(2 * l - 1)) * s,
			x = c * (1 - Math.abs((h / 60) % 2 - 1)),
			m = l - c/2,
			r = 0,
			g = 0,
			b = 0;
		if (0 <= h && h < 60) {
			r = c; g = x; b = 0;
		} else if (60 <= h && h < 120) {
			r = x; g = c; b = 0;
		} else if (120 <= h && h < 180) {
			r = 0; g = c; b = x;
		} else if (180 <= h && h < 240) {
			r = 0; g = x; b = c;
		} else if (240 <= h && h < 300) {
			r = x; g = 0; b = c;
		} else if (300 <= h && h < 360) {
			r = c; g = 0; b = x;
		}
		r = Math.round((r + m) * 255);
		g = Math.round((g + m) * 255);
		b = Math.round((b + m) * 255);
		return RGBToHex(r,g,b);
	}
	for (let t=0;t<6;t=t+1)
	{
		SetupSphere(scene, t, HSLToRGB(t*256/6,255,255));
		break;
	}
	SetupPlane(scene, 0, 0x9999ff);

	function resizeRendererToDisplaySize(renderer) {
		const canvas = renderer.domElement;
		const width = canvas.clientWidth;
		const height = canvas.clientHeight;
		const needResize = canvas.width !== width || canvas.height !== height;
		if (needResize) {
			renderer.setSize(width, height, false);
		}
		return needResize;
	}
	function render(time) {
		time *= 0.001;  // convert time to seconds

		if (resizeRendererToDisplaySize(renderer)) {
			const canvas = renderer.domElement;
			camera.aspect = canvas.clientWidth / canvas.clientHeight;
			camera.updateProjectionMatrix();
		}

		//cube.rotation.x = time;
		//cube.rotation.y = time;

		renderer.render(scene, camera);

		requestAnimationFrame(render);
	}
	requestAnimationFrame(render);
}

main();