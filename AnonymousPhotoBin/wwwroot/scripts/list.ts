interface IFileMetadata {
    fileMetadataId: string;
    width: number;
    height: number;
    takenAt: Date | string | null;
    uploadedAt: Date | string;
    originalFilename: string;
    userName: string | null;
    category: string | null;
    contentType: string;
}

class FileModel implements IFileMetadata {
    readonly fileMetadataId: string;
    readonly width: number;
    readonly height: number;
    readonly takenAt: Date | null;
    readonly uploadedAt: Date;
    readonly originalFilename: string;
    readonly userName: string | null;
    readonly category: string | null;
    readonly contentType: string;

    readonly url: string;
    readonly thumbnailUrl: string;
    checked: KnockoutObservable<boolean>;

    constructor(m: IFileMetadata) {
        this.fileMetadataId = m.fileMetadataId;
        this.width = m.width;
        this.height = m.height;
        this.takenAt = m.takenAt != null
            ? new Date(m.takenAt)
            : null;
        this.uploadedAt = new Date(m.uploadedAt);
        this.originalFilename = m.originalFilename;
        this.userName = m.userName;
        this.category = m.category;
        this.contentType = m.contentType;

        this.url = `/api/files/${this.fileMetadataId}`;
        this.thumbnailUrl = `/api/thumbnails/${this.fileMetadataId}`;
        this.checked = ko.observable(false);
    }
}

class ListViewModel {
    private files: FileModel[];

    constructor(files: IFileMetadata[]) {
        this.files = files.map(f => new FileModel(f));
    }
}

document.addEventListener("DOMContentLoaded", async () => {
    let resp = await fetch("/api/files");
    let files = await resp.json();
    ko.applyBindings(new ListViewModel(files), document.getElementById("ko-area"));
}, false);
