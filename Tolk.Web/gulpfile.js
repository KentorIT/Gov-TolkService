/// <binding BeforeBuild='less' ProjectOpened='watch' />
var gulp = require("gulp");
var less = require("gulp-less");

gulp.task("less", function () {
    return gulp.src("wwwroot/css/site.less")
        .pipe(less())
        .pipe(gulp.dest("wwwroot/css"));
});

gulp.task("watch", function () {
    return gulp.watch("wwwroot/css/site.less", ["less"]);
});
