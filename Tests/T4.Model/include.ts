export function format(formatString: string) {
	return function (target, propertyKey: string) {
		console.log("format(): called");
	}
}

export function getFormat(target: any, propertyKey: string) {
	//return Reflect.getMetadata(formatMetadataKey, target, propertyKey);
}

export interface ISomething {
	something(val?: number);
}

export function g() {
	console.log("g(): evaluated");
	return function (target, propertyKey: string, descriptor: PropertyDescriptor) {
		console.log("g(): called");
	}
}

export function sealed(constructor?: Function) {
	Object.seal(constructor);
	Object.seal(constructor.prototype);
}

export function configurable(value: boolean) {
	return function(target: any, propertyKey: string, descriptor: PropertyDescriptor) {
		descriptor.configurable = value;
	};
}
