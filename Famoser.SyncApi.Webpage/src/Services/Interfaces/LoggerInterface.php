<?php
/**
 * Created by PhpStorm.
 * User: famoser
 * Date: 14.11.2016
 * Time: 12:42
 */

namespace Famoser\SyncApi\Services\Interfaces;


interface LoggerInterface
{
    /**
     * log your message
     *
     * @param $message
     * @param $filename
     * @param bool $clearOld
     */
    public function log($message, $filename, $clearOld = true);

    /**
     * get path where the log files are saved
     * 
     * @return string
     */
    public function getLogPath();
}