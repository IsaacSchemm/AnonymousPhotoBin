interface Window {
    alertify: any;
    bootbox: any;
    BootstrapDialog: any;
}

function alertAsync(str: string): Promise<void> | void {
    return window.bootbox ? new Promise(resolve => window.bootbox.alert({
        message: str,
        callback: () => resolve(),
        animate: false
    }))
    : alert(str);
}

function promptAsync(str: string, _default?: string): Promise<string | null> | string | null {
    return window.bootbox ? new Promise(resolve => window.bootbox.prompt({
        title: str,
        value: _default,
        callback: (s: string) => resolve(s),
        animate: false
    }))
    : prompt(str, _default);
}

function confirmAsync(str: string): Promise<boolean> | boolean {
    return window.bootbox ? new Promise(resolve => window.bootbox.confirm({
        message: str,
        callback: (b: boolean) => resolve(b),
        animate: false
    }))
    : confirm(str);
}
