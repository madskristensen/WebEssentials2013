declare module server {
	interface Simple {
		ASimple: server.Simple;
		AString: string;
		ABool: boolean;
		AnInt: number;
		ADateTime: Date;
	}
}
