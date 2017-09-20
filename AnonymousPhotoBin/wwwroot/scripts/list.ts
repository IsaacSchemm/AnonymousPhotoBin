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
    newFilename: string;
}

class FileModel implements IFileMetadata {
    readonly fileMetadataId: string;
    readonly width: number;
    readonly height: number;
    readonly takenAt: Date | null;
    readonly uploadedAt: Date;
    readonly originalFilename: string;
    readonly userName: string;
    readonly category: string;
    readonly contentType: string;
    readonly newFilename: string;

    readonly url: string;
    readonly thumbnailUrl: string;
    checked: KnockoutObservable<boolean>;

    readonly takenAtStr: KnockoutComputed<string | null>;
    readonly uploadedAtStr: KnockoutComputed<string>;

    constructor(m: IFileMetadata) {
        this.fileMetadataId = m.fileMetadataId;
        this.width = m.width;
        this.height = m.height;
        this.takenAt = m.takenAt != null
            ? new Date(m.takenAt)
            : null;
        this.uploadedAt = new Date(m.uploadedAt);
        this.originalFilename = m.originalFilename;
        this.userName = m.userName || "";
        this.category = m.category || "";
        this.contentType = m.contentType;
        this.newFilename = m.newFilename;

        this.url = `/api/files/${this.fileMetadataId}`;
        this.thumbnailUrl = `/api/thumbnails/${this.fileMetadataId}`;
        this.checked = ko.observable(false);

        this.takenAtStr = ko.pureComputed(() => this.takenAt && this.takenAt.toLocaleString());
        this.uploadedAtStr = ko.pureComputed(() => this.uploadedAt && this.uploadedAt.toLocaleString());
    }

    toggle() {
        this.checked(!this.checked());
    }

    defaultAction(f: FileModel, e: Event) {
        return true;
    }
}

class ListViewModel {
    readonly files: FileModel[];
    readonly myTimeZone: string;

    readonly startDate: KnockoutObservable<string>;
    readonly endDate: KnockoutObservable<string>;
    readonly userName: KnockoutObservable<string>;
    readonly category: KnockoutObservable<string>;
    readonly viewStyle: KnockoutObservable<string>;

    readonly userNames: KnockoutObservable<string[]>;
    readonly categories: KnockoutObservable<string[]>;
    readonly displayedFiles: KnockoutComputed<FileModel[]>;

    constructor(files: IFileMetadata[]) {
        this.files = files.map(f => new FileModel(f)).sort((a, b) => +(a.takenAt || a.uploadedAt) - +(b.takenAt || b.uploadedAt));

        try {
            this.myTimeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
        } catch (e) {
            this.myTimeZone = `UTC + ${new Date().getTimezoneOffset()} minutes`;
        }

        this.startDate = ko.observable("2000-01-01T00:00");
        this.endDate = ko.observable(`${new Date().getFullYear() + 1}-01-01T00:00`);
        this.userName = ko.observable("");
        this.category = ko.observable("");
        this.viewStyle = ko.observable("Table");

        this.userNames = ko.pureComputed(() => this.files.map(f => f.userName).filter((v, i, a) => a.indexOf(v) === i).sort());
        this.categories = ko.pureComputed(() => this.files.map(f => f.category).filter((v, i, a) => a.indexOf(v) === i).sort());
        this.displayedFiles = ko.pureComputed(() => this.files.filter(f => {
            if (this.startDate() && new Date(this.startDate()) > (f.takenAt || f.uploadedAt)) return false;
            if (this.endDate() && new Date(this.endDate()) < (f.takenAt || f.uploadedAt)) return false;
            if (this.userName() && f.userName != this.userName()) return false;
            if (this.category() && f.category != this.category()) return false;
            return true;
        }));
    }
}

var vm: ListViewModel;

document.addEventListener("DOMContentLoaded", async () => {
    let resp = await fetch("/api/files");
    let files = await resp.json();
    ko.applyBindings(vm = new ListViewModel(files), document.getElementById("ko-area"));
}, false);
