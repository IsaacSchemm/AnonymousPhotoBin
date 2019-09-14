///<reference path="../../node_modules/@types/jquery/index.d.ts"/>
///<reference path="../../node_modules/@types/jquery.fileupload/index.d.ts"/>
///<reference path="../../node_modules/@types/knockout/index.d.ts"/>

interface Iterable<T> { }

interface IExistingPhotoMetadata {
    id: string;
    takenAt: Date | string | null;
    uploadedAt: Date | string;
    originalFilename: string;
    userName: string | null;
    category: string | null;
    size: number;
    url: string;
    newFilename: string;
}
