Date.prototype.equalsDate = function (otherDate) {
    return (
        this.getFullYear() === otherDate.getFullYear() &&
        this.getMonth() === otherDate.getMonth() &&
        this.getDate() === otherDate.getDate()
    );
};

Date.prototype.equalsDateTime = function (otherDate) {
    return (
        this.equalsDate(otherDate) &&
        this.getHours() === otherDate.getHours() &&
        this.getMinutes() === otherDate.getMinutes() &&
        this.getSeconds() === otherDate.getSeconds()
    );
};

//Date.prototype.gmtToLocalTime = function () {
//    var timezoneOffsetMillis = new Date().getTimezoneOffset() * 60 * 1000;
//    this.setTime(this.getTime() - timezoneOffsetMillis);
//    return this;
//};